using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using System.Collections.Generic;
using SQLite;

namespace The_Better_Notepad
{
    [Activity(Label = "Better Notepad", MainLauncher = true, Icon = "@drawable/icon", Theme="@style/MyTheme")]
    public class MainActivity : ActionBarActivity
    {
        //private LinearLayout theBase;
        private ListView mListview;
        private List<Note> noteHolder;
        private Android.Support.V7.Widget.Toolbar mToolbar;
        private string docsFolder;
        //private int themeSelection;
        public static string pathToDatabase; // Make static so you can grab the DB path from where ever

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            //theBase = FindViewById<LinearLayout>(Resource.Id.theBase);

            // Create the DB on start of application
            docsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            pathToDatabase = System.IO.Path.Combine(docsFolder, "manager_db.db");
            createTheDataBase(pathToDatabase);

            // Set up the custom toolbar
            mToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.main_toolbar);
            SetSupportActionBar(mToolbar);
            SupportActionBar.Title = "Better Notepad";

            // Set up the list view
            mListview = FindViewById<ListView>(Resource.Id.theListView);
         
            // Set up the note holder
            noteHolder = new List<Note>();

            // Do the adapter stuff
            refreshAdapter();

            mListview.ItemClick += openListRow;

            mListview.ItemLongClick += deletePrompt;
        }

        private void deletePrompt(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            string id = noteHolder[e.Position].Id.ToString();
            AlertDialog.Builder selection = new AlertDialog.Builder(this);

            selection.SetTitle("Delete?");
            selection.SetMessage("Would You Like To Delete This Note?");

            selection.SetPositiveButton("Yes", (s, args) =>
            {
                deleteNote(id);
                Toast.MakeText(this, "Deleted", ToastLength.Short).Show();

                // Refresh the noteHolder and the adapter upon succesfull deletion
                refreshAdapter();
            });

            selection.SetNegativeButton("Cancel", (s, args) =>
            {
                Console.WriteLine("Hehehe");
            });
            selection.Show();
        }

        /// <summary>
        /// So what we do here is determine what kind of note is to be
        /// deleted, if it's a regular text note, we just simply remove it
        /// from the database.
        /// 
        /// If it's an drawing note, we first gotta delete the actual jpg
        /// then we can go ahead and delete it from the database.
        /// </summary>
        /// <param name="id"></param>
        private void deleteNote(string id)
        {
            var db = new SQLiteConnection(pathToDatabase);
            string image_path = db.ExecuteScalar<string>("SELECT ImagePath FROM Note WHERE Id = ?", id);
            if (string.IsNullOrEmpty(image_path))
            {
                SQLiteCommand cmd = db.CreateCommand("DELETE FROM Note WHERE Id = @pID");
                try
                {
                    cmd.Bind("@pID", id);
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException error)
                {
                    Toast.MakeText(this, error.Message, ToastLength.Short).Show();
                }
            }
            else
            {
                // First lets delete the actual jpg
                System.IO.File.Delete(image_path);

                // Then lets actually delete it from the database
                SQLiteCommand cmd = db.CreateCommand("DELETE FROM Note WHERE ImagePath = @pPATH");
                try
                {
                    cmd.Bind("@pPATH", image_path);
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException e)
                {
                    Toast.MakeText(this, e.Message, ToastLength.Short).Show();
                }
            }
        }

        private void openListRow(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Get the ID num
            string id = noteHolder[e.Position].Id.ToString();
            string path = noteHolder[e.Position].ImagePath;
            
            // If there isn't a 'path' its not an image note, so load up the reg version
            if (string.IsNullOrEmpty(path))
            {
                // Create a new intent
                Intent intent = new Intent(this, typeof(NoteActivity));

                // Push the data into the new intent
                intent.PutExtra("the_id", id);
                this.StartActivity(intent);
            }
            else
            {
                Intent intent = new Intent(this, typeof(DrawingNoteActivity));
                intent.PutExtra("the_id", id);
                intent.PutExtra("the_path", path);
                this.StartActivity(intent);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_action_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_add:
                    //The user has selected to add a note
                    Intent intent = new Intent(this, typeof(NoteActivity));
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.action_settings:
                    Intent intent2 = new Intent(this, typeof(DrawingNoteActivity));
                    this.StartActivity(intent2);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void createTheDataBase(string path)
        {
            // Create the database if it does not aleady exist, this is called on startup of the application
            if (!System.IO.File.Exists(path))
            {
                var db = new SQLiteConnection(path);
                db.CreateTable<Note>();
            }
        }

        private List<Note> fetchData(string path)
        {
            var db = new SQLiteConnection(path);
            var holder = db.Query<Note>("SELECT * FROM Note ORDER BY Id DESC");
            return holder;
        }

        protected override void OnResume()
        {
            base.OnResume();
            refreshAdapter();
        }

        private void refreshAdapter()
        {
            noteHolder = fetchData(pathToDatabase);
            NoteAdapter adapter = new NoteAdapter(this, Resource.Layout.row_layout, noteHolder, mListview);
            mListview.Adapter = adapter;
        }
    }
}

