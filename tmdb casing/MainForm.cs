﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nikse.SubtitleEdit.PluginLogic;
using WatTmdb.V3;

namespace tmdb_casing
{
    public partial class MainForm : Form
    {
        private Subtitle _subtitle;
        private string _name;
        private string _description;
        private Form _subtitleEditForm;
        private List<string> Names;

        public string FixedSubtitle { get; private set; }
        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(Subtitle sub, string name, string description, Form parentForm)
            : this()
        {
            // TODO: Complete member initialization
            this._subtitle = sub;
            this._name = name;
            this._description = description;
            this._subtitleEditForm = parentForm;
            
            SearchFormMovie();
        }

        private void SearchFormMovie()
        {
            using (var searchForm = new MovieSeacher())
            {
                if (searchForm.ShowDialog() == DialogResult.OK)
                {
                }
            }
        }

        private void AddResultToListview()
        {

        }

        private void ChangeNames()
        {
            foreach (Paragraph p in _subtitle.Paragraphs)
            {
            }
        }
    }
}
