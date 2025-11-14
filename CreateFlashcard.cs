using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient; //
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static iText.IO.Image.Jpeg2000ImageData;

namespace QuizMe_
{
    public partial class CreateFlashcard : Form
    {
        SqlConnection con = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"); //

        // --- ADD THIS ---
        // This list will hold your study sets for the dropdown
        private List<StudySetItem> studySets = new List<StudySetItem>();

        public CreateFlashcard()
        {
            InitializeComponent();
            // --- ADD THIS ---
            // We will load the study sets when the form is created
            LoadStudySets();
        }

        // --- ADD THIS NEW HELPER CLASS ---
        // This class will store the ID and Title for the ComboBox
        private class StudySetItem
        {
            public int StudySetID { get; set; }
            public string Title { get; set; }

            // This tells the ComboBox what text to display
            public override string ToString()
            {
                return Title;
            }
        }

        // --- ADD THIS NEW METHOD ---
        private void LoadStudySets()
        {
            try
            {
                cmbStudySets.Items.Clear();
                studySets.Clear();

                // Add a "None" option as the default
                studySets.Add(new StudySetItem { StudySetID = 0, Title = "None (General Flashcard)" });

                // Get all study sets for the logged-in user
                string query = "SELECT StudySetID, Title FROM StudySets WHERE UserID = @UserID";

                // Use a new connection to be safe
                using (SqlConnection studySetCon = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"))
                {
                    using (SqlCommand cmd = new SqlCommand(query, studySetCon))
                    {
                        cmd.Parameters.AddWithValue("@UserID", QuizMe_.SignIn.staticUserID); // Uses your existing static user ID
                        studySetCon.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            studySets.Add(new StudySetItem
                            {
                                StudySetID = (int)reader["StudySetID"],
                                Title = reader["Title"].ToString()
                            });
                        }
                    }
                }

                // Tell the ComboBox to use our list
                cmbStudySets.DataSource = studySets;
                cmbStudySets.DisplayMember = "Title";
                cmbStudySets.ValueMember = "StudySetID";
                cmbStudySets.SelectedIndex = 0; // Select "None" by default
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading study sets: " + ex.Message);
            }
        }


        private void backBtn_Click(object sender, EventArgs e) //
        {
            Flashcards flashcards = new Flashcards();
            this.Hide();
            flashcards.Show();
        }

        private void buttonSubmit_Click(object sender, EventArgs e) //
        {

            if (string.IsNullOrWhiteSpace(txtQuestion.Text) || string.IsNullOrWhiteSpace(txtAnswer.Text)) //
            {
                MessageBox.Show("Please enter both a question and an answer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); //
                return; //
            }

            try
            {
                con.Open();

                // --- MODIFY THIS QUERY ---
                // Add the new columns: StudySetID and Status
                string query = "INSERT INTO Flashcards (user_id, question, answer, schedule_date, StudySetID, Status) VALUES (@user_id, @question, @answer, @schedule_date, @StudySetID, 0)"; //

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@user_id", QuizMe_.SignIn.staticUserID); //
                    cmd.Parameters.AddWithValue("@question", txtQuestion.Text); //
                    cmd.Parameters.AddWithValue("@answer", txtAnswer.Text); //
                    cmd.Parameters.AddWithValue("@schedule_date", dtpScheduleDate.Value); //

                    // --- ADD THIS ---
                    // Get the selected Study Set from the ComboBox
                    StudySetItem selectedSet = (StudySetItem)cmbStudySets.SelectedItem;

                    if (selectedSet == null)
                    {
                        MessageBox.Show("Error: No study set was selected. Please try again.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        con.Close(); // Close the connection before returning
                        return; // Stop the method here to prevent the crash
                    }
                    // --- END OF NEW CHECK ---

                    if (selectedSet.StudySetID == 0)
                    {
                        // If "None" is selected, save NULL to the database
                        
                        cmd.Parameters.AddWithValue("@StudySetID", DBNull.Value);
                    }
                    else
                    {
                       cmd.Parameters.AddWithValue("@StudySetID", selectedSet.StudySetID);
                     }

                    cmd.ExecuteNonQuery(); //
                }

                MessageBox.Show("Flashcard saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); //


                txtQuestion.Clear(); //
                txtAnswer.Clear(); //
                dtpScheduleDate.Value = DateTime.Now; //
                cmbStudySets.SelectedIndex = 0; // Reset ComboBox to "None"
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //
            }
            finally
            {
                if (con.State == System.Data.ConnectionState.Open) //
                {
                    con.Close(); //
                }
            }
        }
    }
}