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
using Square.Picasso;

namespace The_Better_Notepad
{
    class NoteAdapter : BaseAdapter<Note>
    {
        private Context mContext;
        private int mRowLayout;
        private List<Note> mNotes;
        private ListView theListView;

        public NoteAdapter(Context context, int rowLayout, List<Note> notes, ListView listView)
        {
            mContext = context;
            mRowLayout = rowLayout;
            mNotes = notes;
            theListView = listView;
        }
        public override int Count
        {
            get { return mNotes.Count; }
        }

        public override Note this[int position]
        {
            get { return mNotes[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View row = convertView;

            if (row == null)
            {
                row = LayoutInflater.From(mContext).Inflate(mRowLayout, parent, false);
                CheckBox checkBox = row.FindViewById<CheckBox>(Resource.Id.checkBox1);
                checkBox.Focusable = false;
                checkBox.FocusableInTouchMode = false;
                checkBox.Clickable = false;
                checkBox.Alpha = 0;
            }

            TextView title = row.FindViewById<TextView>(Resource.Id.row_title);
            TextView blurb = row.FindViewById<TextView>(Resource.Id.row_blurb);
            TextView date = row.FindViewById<TextView>(Resource.Id.row_date);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.row_image);
            

            var the_uri = Android.Net.Uri.Parse("file://" + mNotes[position].ImagePath);
            Picasso.With(mContext)
                .Load(the_uri)
                .Resize(700, 300)
                .Into(image);

            title.Text = mNotes[position].Title;
            blurb.Text = mNotes[position].Blurb;
            date.Text = mNotes[position].Date.ToString();

            return row;
        }
    }
}