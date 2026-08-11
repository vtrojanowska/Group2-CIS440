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
        public int support_count { get; set; }
        public string manager_response { get; set; }
        public string response_date { get; set; }
    }

    public class FollowupMessage
    {
        public int id { get; set; }
        public int submission_id { get; set; }
        public string author_type { get; set; }
        public string message { get; set; }
        public string created_on { get; set; }
    }

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class ProjectServices : WebService
    {
        // Keep the real password only on your local computer.
        private string dbID = "cis440sum26team2";
        private string dbPass = "PUT_YOUR_TEAM_PASSWORD_HERE";
        private string dbName = "cis440sum26team2";

        // Demo-only manager code for the class prototype.
        private string managerCode = "workvoice2";

        private string getConString()
        {
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" +
                dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
        }

        private bool ManagerIsLoggedIn()
        {
            return Session["isManager"] != null &&
                Convert.ToBoolean(Session["isManager"]);
        }

        [WebMethod(EnableSession = true)]
        public string TestConnection()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
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
        public bool ManagerLogin(string code)
        {
            if (code == managerCode)
            {
                Session["isManager"] = true;
                return true;
            }

            Session["isManager"] = false;
            return false;
        }

        [WebMethod(EnableSession = true)]
        public void ManagerLogout()
        {
            Session["isManager"] = false;
        }

        [WebMethod(EnableSession = true)]
        public bool IsManager()
        {
            return ManagerIsLoggedIn();
        }

        [WebMethod(EnableSession = true)]
        public int SubmitFeedback(
            string category,
            string concern,
            string solution,
            string submitterToken)
        {
            if (string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(concern) ||
                string.IsNullOrWhiteSpace(solution) ||
                string.IsNullOrWhiteSpace(submitterToken))
            {
                return -1;
            }

            if (category.Length > 50 ||
                concern.Length > 600 ||
                solution.Length > 600 ||
                submitterToken.Length > 64)
            {
                return -1;
            }

            try
            {
                string query =
                    "INSERT INTO submissions " +
                    "(category, concern, solution, status, submitter_token) " +
                    "VALUES (@category, @concern, @solution, 'Submitted', @token)";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@category", category);
                    cmd.Parameters.AddWithValue("@concern", concern);
                    cmd.Parameters.AddWithValue("@solution", solution);
                    cmd.Parameters.AddWithValue("@token", submitterToken);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return Convert.ToInt32(cmd.LastInsertedId);
                }
            }
            catch
            {
                return -1;
            }
        }

        [WebMethod(EnableSession = true)]
        public FeedbackSubmission[] GetSubmissions()
        {
            List<FeedbackSubmission> submissions =
                new List<FeedbackSubmission>();

            try
            {
                string query =
                    "SELECT id, category, concern, solution, status, support_count, " +
                    "DATE_FORMAT(submitted_on, '%m/%d/%Y') AS submitted_on, " +
                    "manager_response, " +
                    "IFNULL(DATE_FORMAT(response_date, '%m/%d/%Y'), '') AS response_date " +
                    "FROM submissions ORDER BY id DESC";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FeedbackSubmission submission =
                                new FeedbackSubmission();

                            submission.id = Convert.ToInt32(reader["id"]);
                            submission.category = reader["category"].ToString();
                            submission.concern = reader["concern"].ToString();
                            submission.solution = reader["solution"].ToString();
                            submission.status = reader["status"].ToString();
                            submission.submitted_on = reader["submitted_on"].ToString();
                            submission.support_count =
                                Convert.ToInt32(reader["support_count"]);
                            submission.manager_response =
                                reader["manager_response"] == DBNull.Value
                                    ? ""
                                    : reader["manager_response"].ToString();
                            submission.response_date = reader["response_date"].ToString();

                            submissions.Add(submission);
                        }
                    }
                }
            }
            catch
            {
                return new FeedbackSubmission[0];
            }

            return submissions.ToArray();
        }

        [WebMethod(EnableSession = true)]
        public bool SupportIdea(int submissionId, string supporterToken)
        {
            if (submissionId <= 0 ||
                string.IsNullOrWhiteSpace(supporterToken) ||
                supporterToken.Length > 64)
            {
                return false;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    using (MySqlTransaction transaction = con.BeginTransaction())
                    {
                        string insertQuery =
                            "INSERT IGNORE INTO submission_supports " +
                            "(submission_id, supporter_token) " +
                            "VALUES (@submissionId, @token)";

                        using (MySqlCommand insertCmd =
                            new MySqlCommand(insertQuery, con, transaction))
                        {
                            insertCmd.Parameters.AddWithValue(
                                "@submissionId",
                                submissionId
                            );

                            insertCmd.Parameters.AddWithValue(
                                "@token",
                                supporterToken
                            );

                            int rowsAdded = insertCmd.ExecuteNonQuery();

                            if (rowsAdded == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        string updateQuery =
                            "UPDATE submissions " +
                            "SET support_count = support_count + 1 " +
                            "WHERE id = @submissionId";

                        using (MySqlCommand updateCmd =
                            new MySqlCommand(updateQuery, con, transaction))
                        {
                            updateCmd.Parameters.AddWithValue(
                                "@submissionId",
                                submissionId
                            );

                            updateCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool UpdateSubmissionStatus(int id, string status)
        {
            if (!ManagerIsLoggedIn())
            {
                return false;
            }

            string[] allowedStatuses =
            {
                "Submitted",
                "Under Review",
                "Planned",
                "Completed"
            };

            if (Array.IndexOf(allowedStatuses, status) < 0)
            {
                return false;
            }

            try
            {
                string query =
                    "UPDATE submissions SET status = @status WHERE id = @id";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool AddManagerResponse(int id, string response)
        {
            if (!ManagerIsLoggedIn() ||
                id <= 0 ||
                string.IsNullOrWhiteSpace(response) ||
                response.Length > 1000)
            {
                return false;
            }

            try
            {
                string query =
                    "UPDATE submissions " +
                    "SET manager_response = @response, response_date = NOW() " +
                    "WHERE id = @id";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@response", response);
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool AddEmployeeFollowup(
            int submissionId,
            string message,
            string authorToken)
        {
            if (submissionId <= 0 ||
                string.IsNullOrWhiteSpace(message) ||
                message.Length > 1000 ||
                string.IsNullOrWhiteSpace(authorToken))
            {
                return false;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string checkQuery =
                        "SELECT COUNT(*) FROM submissions " +
                        "WHERE id = @id AND submitter_token = @token";

                    using (MySqlCommand checkCmd =
                        new MySqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", submissionId);
                        checkCmd.Parameters.AddWithValue("@token", authorToken);

                        int matches = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (matches == 0)
                        {
                            return false;
                        }
                    }

                    string insertQuery =
                        "INSERT INTO followups " +
                        "(submission_id, author_type, message, author_token) " +
                        "VALUES (@id, 'Employee', @message, @token)";

                    using (MySqlCommand insertCmd =
                        new MySqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@id", submissionId);
                        insertCmd.Parameters.AddWithValue("@message", message);
                        insertCmd.Parameters.AddWithValue("@token", authorToken);

                        return insertCmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool AddManagerFollowup(int submissionId, string message)
        {
            if (!ManagerIsLoggedIn() ||
                submissionId <= 0 ||
                string.IsNullOrWhiteSpace(message) ||
                message.Length > 1000)
            {
                return false;
            }

            try
            {
                string query =
                    "INSERT INTO followups " +
                    "(submission_id, author_type, message) " +
                    "VALUES (@id, 'Manager', @message)";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", submissionId);
                    cmd.Parameters.AddWithValue("@message", message);

                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        public FollowupMessage[] GetFollowups(int submissionId)
        {
            List<FollowupMessage> messages = new List<FollowupMessage>();

            try
            {
                string query =
                    "SELECT id, submission_id, author_type, message, " +
                    "DATE_FORMAT(created_on, '%m/%d/%Y %h:%i %p') AS created_on " +
                    "FROM followups " +
                    "WHERE submission_id = @id " +
                    "ORDER BY created_on ASC, id ASC";

                using (MySqlConnection con = new MySqlConnection(getConString()))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", submissionId);
                    con.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FollowupMessage item = new FollowupMessage();
                            item.id = Convert.ToInt32(reader["id"]);
                            item.submission_id =
                                Convert.ToInt32(reader["submission_id"]);
                            item.author_type = reader["author_type"].ToString();
                            item.message = reader["message"].ToString();
                            item.created_on = reader["created_on"].ToString();
                            messages.Add(item);
                        }
                    }
                }
            }
            catch
            {
                return new FollowupMessage[0];
            }

            return messages.ToArray();
        }
    }
}