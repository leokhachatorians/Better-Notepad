using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using Android.Support.V7.App;
using Java.Lang;
using SQLite;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace The_Better_Notepad
{
    [Activity(Label = "Better Than Notepad", Theme = "@style/NoteTheme")]
    class DrawingNoteActivity : ActionBarActivity
    {
        //ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape
        private Android.Support.V7.Widget.Toolbar mToolbar;
        private LinearLayout mContainer;
        private DrawView mCanvas;
        private string mPath;
        private string mImagesLocation;
        private string mID;
        private string mDrawCheck; // This will be used to determine which action bar to display.

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.DrawingNoteLayout);
            
            // Set up our location of where we want to store our images
            mImagesLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            // Initialize our view
            mContainer = FindViewById<LinearLayout>(Resource.Id.draw_container);

            //Set up the custom toolbar
            mToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.draw_toolbar);
            SetSupportActionBar(mToolbar);

            if (Intent.GetStringExtra("the_path") != null) // Get passed data if it exists
            {
                // Set the orientation to vertical to read note on glance
                this.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;

                // Set check to no; user is not able to draw
                mDrawCheck = "no";

                SupportActionBar.Title = "Modify Your Note";
                mContainer.RemoveAllViews();

                // Get the passed ID
                mID = Intent.GetStringExtra("the_id");
                mPath = Intent.GetStringExtra("the_path");
                var the_uri = Android.Net.Uri.Parse("file://"+mPath);

                Bitmap mBitmapSource = Android.Provider.MediaStore.Images.Media.GetBitmap(this.ContentResolver, the_uri);

                ImageView mImage = new ImageView(this);
                mImage.SetImageBitmap(mBitmapSource);

                // Dispose of the bitmap and call garbage collection
                // Yeah I guess you shouldn't need to manually collect
                // the garbage, but honestly the slight lag on such a
                // simplistic app doesn't really make a noticable
                // difference, you can just get rid of the collection
                // but I'd rather not have to deal with any memory issues
                mBitmapSource.Dispose();
                GC.Collect();

                mContainer.AddView(mImage);            
            }
            else 
            {
                // Set the orientation to landscape, easier to write in.
                this.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;

                // Set check to yes; the user will be able to draw onto canvas
                mDrawCheck = "yes";
                SupportActionBar.Title = "Draw Your Note";

                // Create the canvas and add it to our container view
                mCanvas = new DrawView(this);
                mContainer.AddView(mCanvas);
                mCanvas.start();
            }
        }

        public override void OnBackPressed()
        {
            this.Finish();
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (mDrawCheck == "yes")
            {
                MenuInflater.Inflate(Resource.Menu.draw_note_action_menu, menu);
                return base.OnCreateOptionsMenu(menu);
            }
            else
            {
                MenuInflater.Inflate(Resource.Menu.display_draw_note_action_menu, menu);
                return base.OnCreateOptionsMenu(menu);
            }
            
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_draw_save:
                    // Create the random title
                    var generated_title = createRandomString();

                    // Grab the bitmap from the current canvas
                    Bitmap bitmap = mCanvas.getBitmap();

                    // Get the path of where to save the image
                    mPath = System.IO.Path.Combine(mImagesLocation, generated_title);

                    // Create stream, compress, and close out stream
                    var stream = new System.IO.FileStream(mPath, System.IO.FileMode.Create);
                    bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    stream.Close();

                    createNote(mPath);
                    Finish();
                    return true;
                case Resource.Id.action_draw_erase:
                    mCanvas.clear();
                    return true;
                case Resource.Id.action_display_draw_delete:
                    deleteFile(mPath);
                    Toast.MakeText(this, "Deleted", ToastLength.Short).Show();
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void createNote(string file_path)
        {
            /// <summary>
            ///     Create the actual note file with only the data
            ///     and the passed in parameter: file_path.
            /// <para> 
            ///     Theres no point in anything else since we're only
            ///     going to display the image and date in the listview
            ///     anyway
            /// </para>
            /// </summary>
            DateTime date = DateTime.Now;
            Note note = new Note { Date = date, ImagePath = file_path };
            addDataToDataBase(note);
        }

        private void addDataToDataBase(Note data)
        {
            ///<summary>
            ///     Using the passed in value: data, we just simply
            ///     create a new connection to the database and
            ///     insert -> update it. We make sure to set our
            ///     mID variable to be the new id of the object.
            /// </summary>
            try
            {
                var db = new SQLiteConnection(MainActivity.pathToDatabase);
                db.Insert(data);
                db.Update(data);
                mID = data.Id.ToString();
            }
            catch (SQLite.SQLiteException e)
            {
                // Problem with adding
                Toast.MakeText(this, e.Message, ToastLength.Short).Show();
            }
        }

        private string createRandomString()
        {
            /// <summary>
            ///     Create a random string and append 'jpg' to the end,
            ///     we will use these as the filename to ensure that there
            ///     it is nearly impossible to overwrite drawn notes that
            ///     the user will create
            /// </summary>
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 10)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result + ".png";
        }

        private void deleteFile(string path)
        {
            // First lets delete the actual jpg
            System.IO.File.Delete(path);

            // Then lets actually delete it from the database
            var db = new SQLiteConnection(MainActivity.pathToDatabase);
            SQLiteCommand cmd = db.CreateCommand("DELETE FROM Note WHERE ImagePath = @pPATH");
            try
            {
                cmd.Bind("@pPATH", path);
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                Toast.MakeText(this, e.Message, ToastLength.Short).Show();
            }
        }

    }

    public class DrawView : View
    {
        public DrawView(Context context) : base(context) { }
        private Path drawPath;
        private Paint drawPaint, canvasPaint;
        private uint paintColor = 0xFF660000;
        private Canvas drawCanvas;
        private Bitmap canvasBitmap;
        private Bitmap placeHolderCanvas;
        public void start()
        {
            drawPath = new Path();
            drawPaint = new Paint();
            drawPaint.Color = new Color((int)paintColor);
            drawPaint.AntiAlias = true;
            drawPaint.StrokeWidth = 20;
            drawPaint.SetStyle(Paint.Style.Stroke);
            drawPaint.StrokeJoin = Paint.Join.Round;
            drawPaint.StrokeCap = Paint.Cap.Round;
            canvasPaint = new Paint();
            canvasPaint.Dither = true;
        }

        public void clear()
        {
            drawCanvas.DrawColor(Color.Argb(255, 227, 242, 253));
            Invalidate();
        }

        public Bitmap getBitmap()
        {
            return canvasBitmap;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            //canvasBitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Rgb565);
            canvasBitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb4444);
            placeHolderCanvas = canvasBitmap;

            drawCanvas = new Canvas(canvasBitmap);
            drawCanvas.DrawColor(Color.Argb(255, 227, 242, 253));
        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawBitmap(canvasBitmap, 0, 0, canvasPaint);
            canvas.DrawPath(drawPath, drawPaint);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            float touchX = e.GetX();
            float touchY = e.GetY();
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    drawPath.MoveTo(touchX, touchY);
                    break;
                case MotionEventActions.Move:
                    drawPath.LineTo(touchX, touchY);
                    break;
                case MotionEventActions.Up:
                    drawCanvas.DrawPath(drawPath, drawPaint);
                    drawPath.Reset();
                    break;
                default:
                    return false;
            }
            Invalidate();
            return true;
        }
    }
}