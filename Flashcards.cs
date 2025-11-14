using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace QuizMe_

{

    public partial class Flashcards : Form
    {
        SqlConnection con = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;");

        private List<Flashcard> allFlashcards = new List<Flashcard>();
        private int currentCardIndex = 0;

        private DateTime? _filterDate = null;
        private int? _studySetID = null;
        public Flashcards() //
        {
            // 1. This is the EMPTY constructor.
            // Your Dashboard, Progress, and Settings buttons use this.
            InitializeComponent();
        }
        public Flashcards(int studySetID)
        {
            InitializeComponent();
            _studySetID = studySetID;

            // When studying a set, we don't need the "Create" or "Delete" buttons
            createFlashcardButton.Visible = false;
            btnDelete.Visible = false;
        }
        public Flashcards(DateTime filterDate)
        {
            InitializeComponent();
            _filterDate = filterDate.Date; // Store the date
        }
        private void btnSet_Click(object sender, EventArgs e)
        {
            Settings settings = new Settings();
            this.Hide();

            settings.Show();
        }

        private void btnProg_Click(object sender, EventArgs e)
        {
            Progress progress = new Progress();
            this.Hide();

            progress.Show();
        }

        private void btnSched_Click(object sender, EventArgs e)
        {
            Schedule schedule = new Schedule();
            this.Hide();


            schedule.Show();
        }

        private void btnQui_Click(object sender, EventArgs e)
        {
            Quizzes quizzes = new Quizzes();
            this.Hide();

            quizzes.Show();
        }

        private void btnFla_Click(object sender, EventArgs e)
        {

        }

        private void btnDash_Click(object sender, EventArgs e)
        {
            Dashboard2 dashboard = new Dashboard2();
            this.Hide();

            dashboard.Show();
        }

        private void createFlashcardButton_Click(object sender, EventArgs e)
        {
            CreateFlashcard createFlashcard = new CreateFlashcard();
            this.Hide();

            createFlashcard.Show();
        }



        private void button11_Click(object sender, EventArgs e)
        {

            lblAnswer.Text = "";
        }

        private void lblSeeAnswer_Click(object sender, EventArgs e)
        {
            lblAnswer.Visible = true;
            lblSeeAnswer.Visible = false;
        }

        private void Flashcards_Load(object sender, EventArgs e)
        {
            DeleteExpiredFlashcards();
            LoadFlashcardsFromDB();
            DisplayCurrentCard();
        }
        private void LoadFlashcardsFromDB()
        {
            allFlashcards.Clear(); //
            try
            {
                con.Open(); //

                // --- MODIFY THIS QUERY ---
                // Select the new 'Status' column
                // The WHERE clause is now dynamic
                string query = "SELECT flashcard_id, question, answer, schedule_date, Status FROM Flashcards WHERE user_id = @user_id"; //

                // --- ADD THIS LOGIC ---
                if (_studySetID.HasValue)
                {
                    // 1. If we are studying a set, load ONLY that set's cards
                    query += " AND StudySetID = @StudySetID";
                }
                else if (_filterDate.HasValue)
                {
                    // 2. This is your existing logic for the schedule
                    query += " AND CAST(schedule_date AS DATE) = @schedule_date_filter"; //
                }
                else
                {
                    // 3. If no set AND no date, load "general" cards (not in any set)
                    query += " AND StudySetID IS NULL";
                }
                // --- END OF NEW LOGIC ---

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@user_id", QuizMe_.SignIn.staticUserID); //

                    // --- ADD THIS LOGIC ---
                    if (_studySetID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@StudySetID", _studySetID.Value);
                    }
                    else if (_filterDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@schedule_date_filter", _filterDate.Value); //
                    }
                    // --- END OF NEW LOGIC ---

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) //
                        {
                            allFlashcards.Add(new Flashcard
                            {
                                FlashcardId = Convert.ToInt32(reader["flashcard_id"]), //
                                Question = reader["question"].ToString(), //
                                Answer = reader["answer"].ToString(), //
                                ScheduleDate = reader["schedule_date"] == DBNull.Value //
                                            ? (DateTime?)null
                                            : Convert.ToDateTime(reader["schedule_date"]), //

                                // --- ADD THIS ---
                                Status = Convert.ToInt32(reader["Status"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading flashcards: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //
            }
            finally
            {
                if (con.State == System.Data.ConnectionState.Open) //
                {
                    con.Close(); //
                }
            }

        }
        private void DeleteExpiredFlashcards()
        {
            // We use a NEW connection here to keep it separate and safe
            using (SqlConnection deleteCon = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"))
            {
                try
                {
                    deleteCon.Open();
                    // This query deletes cards for this user that are in the past
                    string query = "DELETE FROM Flashcards WHERE user_id = @user_id AND schedule_date < @current_datetime";

                    using (SqlCommand cmd = new SqlCommand(query, deleteCon))
                    {
                        cmd.Parameters.AddWithValue("@user_id", QuizMe_.SignIn.staticUserID);
                        cmd.Parameters.AddWithValue("@current_datetime", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // If it fails, just log it. Don't stop the form from loading.
                    Console.WriteLine("Error deleting expired cards: " + ex.Message);
                }
            } // The 'using' block automatically closes the connection
        }
        private void DisplayCurrentCard()
        {
            if (allFlashcards.Count > 0)
            {
                // --- ADDED SAFETY CHECK ---
                // If index is out of bounds (e.g., after deleting last card), reset to last card
                if (currentCardIndex >= allFlashcards.Count)
                {
                    currentCardIndex = allFlashcards.Count - 1;
                }
                // --- END OF CHECK ---

                Flashcard currentCard = allFlashcards[currentCardIndex];
                lblQuestion.Text = currentCard.Question;
                lblAnswer.Text = currentCard.Answer;
                lblAnswer.Visible = false;
                lblSeeAnswer.Visible = true;
                lblCardNo.Text = $"card {currentCardIndex + 1} of {allFlashcards.Count}";

                if (currentCard.ScheduleDate.HasValue)
                {
                    // Format the time to "hh:mm tt" (e.g., "02:30 PM")
                    string timeString = currentCard.ScheduleDate.Value.ToString("hh:mm tt");
                    lblScheduledIdentifier.Text = $"📅 Scheduled for {timeString}";
                    lblScheduledIdentifier.Visible = true;
                }
                else
                {
                    lblScheduledIdentifier.Visible = false;
                }
            }
            else
            {
                if (_filterDate.HasValue)
                {
                    lblQuestion.Text = "No flashcards scheduled for this day.";
                }
                else
                {
                    lblQuestion.Text = "No flashcards found.";
                }

                lblAnswer.Text = "";
                lblAnswer.Visible = false;
                lblSeeAnswer.Visible = false;
                lblCardNo.Text = "card 0 of 0";
                lblScheduledIdentifier.Visible = false; // Also hide it here
            }
        }

        private void nextBtn_Click(object sender, EventArgs e)
        {
            if (allFlashcards.Count == 0 || currentCardIndex >= allFlashcards.Count) return;

            // Update status in DB to 2 (Known)
            UpdateCardStatus(allFlashcards[currentCardIndex].FlashcardId, 2);
            allFlashcards[currentCardIndex].Status = 2; // Update local list

            // Move to the next card
            currentCardIndex++;

            // --- ADD THIS ---
            // If we are in a study set, update the progress bar
            if (_studySetID.HasValue)
            {
                UpdateStudySetProgress();
            }

            DisplayCurrentCard();
        }

        private void prevBtn_Click(object sender, EventArgs e)
        {
            if (allFlashcards.Count == 0 || currentCardIndex >= allFlashcards.Count) return;

            // Update status in DB to 1 (Learning)
            UpdateCardStatus(allFlashcards[currentCardIndex].FlashcardId, 1);
            allFlashcards[currentCardIndex].Status = 1;

            // Move this card to the end of the list to see it again
            Flashcard cardToReview = allFlashcards[currentCardIndex];
            allFlashcards.RemoveAt(currentCardIndex);
            allFlashcards.Add(cardToReview);

            // Display the next card (which is at the same index)
            DisplayCurrentCard();
        }

        // --- ENTIRELY NEW METHOD ---
        private void btnDelete_Click(object sender, EventArgs e)
        {
            // 1. Check if there are any cards to delete
            if (allFlashcards.Count == 0)
            {
                MessageBox.Show("There are no flashcards to delete.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Confirm with the user
            DialogResult confirm = MessageBox.Show("Are you sure you want to permanently delete this flashcard?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // 3. Get the ID of the current card
                    int cardIdToDelete = allFlashcards[currentCardIndex].FlashcardId;

                    // 4. Delete from the database
                    // Use a new connection to avoid conflicts
                    using (SqlConnection deleteCon = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"))
                    {
                        deleteCon.Open();
                        // Delete by the unique FlashcardId AND check the user_id for security
                        string query = "DELETE FROM Flashcards WHERE flashcard_id = @flashcard_id AND user_id = @user_id";
                        using (SqlCommand cmd = new SqlCommand(query, deleteCon))
                        {
                            cmd.Parameters.AddWithValue("@flashcard_id", cardIdToDelete);
                            cmd.Parameters.AddWithValue("@user_id", QuizMe_.SignIn.staticUserID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 5. Remove from the local list
                    allFlashcards.RemoveAt(currentCardIndex);

                    // 6. Refresh the display
                    DisplayCurrentCard();
                    MessageBox.Show("Flashcard deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting flashcard: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void UpdateCardStatus(int flashcardId, int status)
        {
            try
            {
                using (SqlConnection updateCon = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"))
                {
                    string query = "UPDATE Flashcards SET Status = @Status WHERE FlashcardId = @FlashcardId AND user_id = @user_id";
                    using (SqlCommand cmd = new SqlCommand(query, updateCon))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@FlashcardId", flashcardId);
                        cmd.Parameters.AddWithValue("@user_id", QuizMe_.SignIn.staticUserID);
                        updateCon.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to update card status: " + ex.Message);
            }
        }

        // --- ADD THIS NEW METHOD ---
        private void UpdateStudySetProgress()
        {
            if (!_studySetID.HasValue) return;

            try
            {
                // 1. Calculate progress from our local list
                int totalCards = allFlashcards.Count;
                if (totalCards == 0) return;

                // Count all cards that are marked as "Known" (Status = 2)
                int knownCards = allFlashcards.Count(c => c.Status == 2);

                // 2. Calculate percentage
                int progress = (int)(((double)knownCards / totalCards) * 100);

                // 3. Update the StudySets table in the database
                using (SqlConnection progressCon = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=QuizMeDB;Trusted_Connection=True;"))
                {
                    string query = "UPDATE StudySets SET Progress = @Progress WHERE StudySetID = @StudySetID AND UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(query, progressCon))
                    {
                        cmd.Parameters.AddWithValue("@Progress", progress);
                        cmd.Parameters.AddWithValue("@StudySetID", _studySetID.Value);
                        cmd.Parameters.AddWithValue("@UserID", QuizMe_.SignIn.staticUserID);
                        progressCon.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to update study set progress: " + ex.Message);
            }
        }
        public class Flashcard
        {
            public int FlashcardId { get; set; } // <-- NEW PROPERTY
            public string Question { get; set; }
            public string Answer { get; set; }
            public DateTime? ScheduleDate { get; set; }

            public int Status { get; set; }
        }
    }
}