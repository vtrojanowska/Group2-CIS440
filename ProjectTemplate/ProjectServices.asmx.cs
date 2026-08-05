using System;
using System.Collections.Generic;
using System.Web.Services;
using MySql.Data.MySqlClient;

namespace ProjectTemplate
{
    public class FeedbackSubmission
    {
        public int id { get; set; }
        public string category { get; set; }
        public string concern { get; set; }
        public string solution { get; set; }
        public string status { get; set; }
        public string submitted_on { get; set; }
    }

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]

    public class ProjectServices : WebService
    {
        private string dbID = "cis440sum26team2";
        private string dbPass = "cis440sum26team2";
        private string dbName = "cis440sum26team2";

        private string getConString()
        {
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" +
                dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
        }

        [WebMethod(EnableSession = true)]
        public string TestConnection()
        {
            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();
                }

                return "Success!";
            }
            catch (Exception e)
            {
                return "Something went wrong. Error: " + e.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool SubmitFeedback(
            string category,
            string concern,
            string solution)
        {
            try
            {
                string query =
                    "INSERT INTO submissions " +
                    "(category, concern, solution, status) " +
                    "VALUES (@category, @concern, @solution, 'Submitted')";

                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    MySqlCommand cmd = new MySqlCommand(query, con);

                    cmd.Parameters.AddWithValue(
                        "@category",
                        category
                    );

                    cmd.Parameters.AddWithValue(
                        "@concern",
                        concern
                    );

                    cmd.Parameters.AddWithValue(
                        "@solution",
                        solution
                    );

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public FeedbackSubmission[] GetSubmissions()
        {
            List<FeedbackSubmission> submissions =
                new List<FeedbackSubmission>();

            string query =
                "SELECT id, category, concern, solution, status, " +
                "DATE_FORMAT(submitted_on, '%m/%d/%Y') AS submitted_on " +
                "FROM submissions ORDER BY id DESC";

            using (MySqlConnection con =
                new MySqlConnection(getConString()))
            {
                MySqlCommand cmd = new MySqlCommand(query, con);

                con.Open();

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        FeedbackSubmission submission =
                            new FeedbackSubmission();

                        submission.id =
                            Convert.ToInt32(reader["id"]);

                        submission.category =
                            reader["category"].ToString();

                        submission.concern =
                            reader["concern"].ToString();

                        submission.solution =
                            reader["solution"].ToString();

                        submission.status =
                            reader["status"].ToString();

                        submission.submitted_on =
                            reader["submitted_on"].ToString();

                        submissions.Add(submission);
                    }
                }
            }

            return submissions.ToArray();
        }

        [WebMethod(EnableSession = true)]
        public bool UpdateSubmissionStatus(
            int id,
            string status)
        {
            string[] allowedStatuses =
            {
                "Submitted",
                "Under Review",
                "In Progress",
                "Resolved"
            };

            if (Array.IndexOf(allowedStatuses, status) < 0)
            {
                return false;
            }

            try
            {
                string query =
                    "UPDATE submissions " +
                    "SET status = @status " +
                    "WHERE id = @id";

                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    MySqlCommand cmd = new MySqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();

                    int rowsChanged = cmd.ExecuteNonQuery();

                    return rowsChanged > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}