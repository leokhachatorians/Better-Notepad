using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using SQLite;

namespace The_Better_Notepad
{
    [Activity(Label = "Better Than Notepad", Theme = "@style/NoteTheme")]
    class NoteActivity : ActionBarActivity
    {
        private Android.Support.V7.Widget.Toolbar mToolbar;
        private EditText mTitle;
        private EditText mBody;
        private string mID;
        private string mDataBlurb;
        private string pathToDatabase;

        // The orginal body and titles are pulled directly from the DB if data exists
        // and the current title and body are set once the user attempts to leave the activity
        private string mOriginalTitle;
        private string mOriginalBody;
        private string mCurrentTitle;
        private string mCurrentBody;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.NoteLayout);

            // Set up the custom toolbar
            mToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.note_toolbar);
            SetSupportActionBar(mToolbar);
            SupportActionBar.Title = "Add a Note";

            // Reference the path to database
            pathToDatabase = MainActivity.pathToDatabase;

            // Set up our variables
            mTitle = FindViewById<EditText>(Resource.Id.note_title);
            mBody = FindViewById<EditText>(Resource.Id.note_body);

            // Get passed data if it exists
            if (Intent.GetStringExtra("the_id") != null)
            {
                SupportActionBar.Title = "Modify Your Note";

                // Get the passed ID
                mID = Intent.GetStringExtra("the_id");

                // Create the database conneciton
                var db = new SQLiteConnection(pathToDatabase);

                // Set up the text display to match whats in the database as
                // there was data passed into the activity
                mOriginalTitle = db.ExecuteScalar<string>("SELECT Title FROM Note WHERE Id = ?", int.Parse(mID));
                mOriginalBody = db.ExecuteScalar<string>("SELECT Body FROM Note WHERE Id = ?", int.Parse(mID));

                mTitle.Text = mOriginalTitle;
                mBody.Text = mOriginalBody;
            }
            else
            {
                // No data was passed thus the state of the original body and title
                // is 'empty', set the text to reflect that
                mOriginalBody = "";
                mOriginalTitle = "";
                mTitle.Text = mOriginalTitle;
                mBody.Text = mOriginalBody;
            }
        }
        public override void OnBackPressed()
        {
            attemptingToLeaveActivity(mID);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.note_action_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_save:
                    // When a user presses the save button, save the data
                    // and then set the orginal body and title to the current
                    // fields, as the new original versions are what have just
                    // been saved.
                    save(mID);
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }
        private void attemptingToLeaveActivity(string ID)
        {
            /// <summary>
            ///     What we want to do is ensure that whenever the user
            ///     is attempting to leave the activity, that either they
            ///     haven't made any changes to the original file, or that
            ///     they have saved their changes, and thus making their
            ///     new saved file the new 'original file'.
            /// 
            ///<para>
            ///     Note that modifying the original state and then modifying
            ///     it once again to mimic the original, be that added and deleting
            ///     whatever was added, will count as not modifying the file.
            /// </para>
            /// </summary>
            mCurrentTitle = mTitle.Text;
            mCurrentBody = mBody.Text;

            if (mCurrentBody != mOriginalBody || mCurrentTitle != mOriginalTitle)
            {
                // Checks and determines if any changes have been made to the body and title since opening
                AlertDialog.Builder notifyChanged = new AlertDialog.Builder(this);
                notifyChanged.SetTitle("Note Has Been Modified");
                notifyChanged.SetMessage("Would You Like To Save Your Changes?");
                notifyChanged.SetPositiveButton("Yes", (sender, args) =>
                {
                    // User selects to save the data
                    save(mID);
                    Finish();
                });
                notifyChanged.SetNegativeButton("No", (sender, args) =>
                {
                    // User opts out of saving their data
                    Finish();
                });
                notifyChanged.SetNeutralButton("Cancel", (sender, args) => {});
                notifyChanged.Show();
            }
            else
            {
                Finish();
            }
        }

        private Note createDataForEntry(string title, string body, string blurb)
        {
            ///<summary>
            ///     Taking the arguments: title, body, and blurb,
            ///     we just create a new Note, and run through a few if's
            ///     to visually reflect the inputted data on the list view.
            /// 
            /// <para>
            ///     We could just pass in whatever was passed, but actually
            ///     im gonna have to look back to this. Prolly a better idea actually.
            /// </para>
            /// </summary>
            Note holder;
            DateTime date = DateTime.Now;

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
            {
                // If the user has not entered a title nor a body
                holder = new Note { Title = "", Body = "", Date = date };

            }
            else if (string.IsNullOrEmpty(title))
            {
                // If The user has not entered a title
                holder = new Note { Title = "", Body = body, Blurb = blurb, Date = date };
            }
            else if (string.IsNullOrEmpty(body))
            {
                // If the user has not entered a body
                holder = new Note { Title = title, Body = "", Date = date };
            }
            else
            {
                holder = new Note { Title = title, Body = body, Blurb = blurb, Date = date };
            }
            return holder;
        }

        private void addDataToDataBase(Note data)
        {
            try
            {
                var db = new SQLiteConnection(pathToDatabase);
                data.Blurb = createTheBlurb(data.Body); // Ensure the 'Blurb' is the most recent version
                db.Insert(data);
                db.Update(data);
                mID = data.Id.ToString();
            }
            catch (SQLite.SQLiteException e)
            {
                //Toast.MakeText(this, "PROBLEM WITH ADDING", ToastLength.Short).Show();
                Toast.MakeText(this, e.Message, ToastLength.Short).Show();
            }
        }

        private string createTheBlurb(string body)
        /// <summary>
        ///     Creates the little blurb that is visible on the listview.
        ///     Called prior to any data being appended into the database.
        /// </summary>
        {
            string blurb;
            if (body.Length < 22)
            {
                blurb = body;
            }
            else
            {
                blurb = body.Substring(0, 16);
            }
            return blurb;
        }

        private bool checkIfExists(string ID)
        {
            ///<summary>
            ///     Check to see if the note already exists, this feeds back into whether
            ///     or not the note should be simply added into the database or first
            ///     deleted then recreated.
            /// </summary>
            var db = new SQLiteConnection(pathToDatabase);
            var count = db.ExecuteScalar<int>("SELECT Count(*) FROM Note WHERE Id = ?", ID);

            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void deleteNote(Note data, string ID)
        {
            ///<summary>
            ///     Delete the note from the database and add it back into the
            ///     database. Doing this ensures that while the note itself is unchanged,
            ///     the ID of the note is newer, and thus will reflect that on the listview.
            /// </summary>
            var db = new SQLiteConnection(pathToDatabase);
            SQLiteCommand cmd = db.CreateCommand("DELETE FROM Note WHERE Id = @pID");
            try
            {
                cmd.Bind("@pID", ID);
                cmd.ExecuteNonQuery();

                addDataToDataBase(data);
            }
            catch (SQLiteException error)
            {
                Toast.MakeText(this, error.Message, ToastLength.Short).Show();
            }
        }

        private void save(string ID)
        {
            ///<summary>
            ///     Both options save, but first check to see if the note already exists,
            ///     it doesn't just simply append the note into the databse.
            ///     If the note does already exist, run the deleteNote method to ensure
            ///     that the note placement on the listview changes to reflect that it
            ///     has been modified and thus should be the at the top of the list.
            /// </summary>
            mDataBlurb = createTheBlurb(mBody.Text);
            Note data = createDataForEntry(mTitle.Text, mBody.Text, mDataBlurb);
            if (!checkIfExists(mID))
            {
                // If the note does not exist within the databse, create the note
                addDataToDataBase(data);
                Toast.MakeText(this, "Created Your Note", ToastLength.Short).Show();
            }
            else
            {
                // The note does exist, delete the pre-existing note and create a new one
                deleteNote(data, mID);
                Toast.MakeText(this, "Updated Your Note", ToastLength.Short).Show();
            }
        }
    }
}