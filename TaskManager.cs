using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace POE2P
{
    /// <summary>
    /// Represents a single cybersecurity task.
    /// </summary>
    public class CyberTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsComplete { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? ReminderDate { get; set; }
    }

    /// <summary>
    /// Manages cybersecurity tasks with MySQL persistence.
    /// Falls back to in-memory storage if DB is unavailable.
    /// </summary>
    public class TaskManager
    {
        private readonly ActivityLog _log;
        private readonly List<CyberTask> _tasks;
        private MySqlConnection _connection;
        private bool _dbAvailable;

        // ── Connection string — update credentials as needed ──────
        private const string ConnectionString =
            "Server=localhost;Database=cyberbot_db;Uid=root;Pwd=;";

        public TaskManager(ActivityLog log)
        {
            _log = log;
            _tasks = new List<CyberTask>();
            _dbAvailable = TryConnectDb();

            if (_dbAvailable)
            {
                EnsureTableExists();
                LoadTasksFromDb();
            }
        }

        // ── Public API ────────────────────────────────────────────

        public void AddTask(string title, string description, int reminderDays = 0)
        {
            DateTime? reminder = reminderDays > 0
                ? (DateTime?)DateTime.Now.AddDays(reminderDays)
                : null;

            var task = new CyberTask
            {
                Title = title,
                Description = description,
                IsComplete = false,
                DateAdded = DateTime.Now,
                ReminderDate = reminder
            };

            if (_dbAvailable)
                InsertTaskToDb(task);
            else
                _tasks.Add(task);

            string logMsg = $"Task added: '{title}'";
            if (reminder.HasValue)
                logMsg += $" (Reminder set for {reminder.Value:dd MMM yyyy})";

            _log.Add(logMsg);
        }

        public void MarkComplete(int index)
        {
            if (index < 0 || index >= _tasks.Count) return;

            _tasks[index].IsComplete = true;

            if (_dbAvailable)
                UpdateTaskInDb(_tasks[index].Id, true);

            _log.Add($"Task completed: '{_tasks[index].Title}'");
        }

        public void DeleteTask(int index)
        {
            if (index < 0 || index >= _tasks.Count) return;

            string title = _tasks[index].Title;

            if (_dbAvailable)
                DeleteTaskFromDb(_tasks[index].Id);

            _tasks.RemoveAt(index);
            _log.Add($"Task deleted: '{title}'");
        }

        public List<CyberTask> GetAll() => _tasks;

        // ── Database Methods ──────────────────────────────────────

        private bool TryConnectDb()
        {
            try
            {
                _connection = new MySqlConnection(ConnectionString);
                _connection.Open();
                return true;
            }
            catch
            {
                // DB unavailable — run in memory-only mode
                return false;
            }
        }

        private void EnsureTableExists()
        {
            string sql = @"CREATE TABLE IF NOT EXISTS tasks (
                id INT AUTO_INCREMENT PRIMARY KEY,
                title VARCHAR(255) NOT NULL,
                description TEXT,
                is_complete TINYINT(1) DEFAULT 0,
                date_added DATETIME DEFAULT CURRENT_TIMESTAMP,
                reminder_date DATETIME NULL
            );";

            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }

        private void LoadTasksFromDb()
        {
            _tasks.Clear();
            string sql = "SELECT * FROM tasks ORDER BY date_added DESC;";

            using var cmd = new MySqlCommand(sql, _connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                _tasks.Add(new CyberTask
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader.IsDBNull("description") ? "" : reader.GetString("description"),
                    IsComplete = reader.GetBoolean("is_complete"),
                    DateAdded = reader.GetDateTime("date_added"),
                    ReminderDate = reader.IsDBNull("reminder_date")
                        ? (DateTime?)null
                        : reader.GetDateTime("reminder_date")
                });
            }
        }

        private void InsertTaskToDb(CyberTask task)
        {
            string sql = @"INSERT INTO tasks (title, description, is_complete, date_added, reminder_date)
                           VALUES (@title, @desc, 0, @added, @reminder);";

            using var cmd = new MySqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@title", task.Title);
            cmd.Parameters.AddWithValue("@desc", task.Description ?? "");
            cmd.Parameters.AddWithValue("@added", task.DateAdded);
            cmd.Parameters.AddWithValue("@reminder", task.ReminderDate.HasValue
                ? (object)task.ReminderDate.Value
                : DBNull.Value);

            cmd.ExecuteNonQuery();
            task.Id = (int)cmd.LastInsertedId;
            _tasks.Add(task);
        }

        private void UpdateTaskInDb(int id, bool complete)
        {
            string sql = "UPDATE tasks SET is_complete = @complete WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@complete", complete ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private void DeleteTaskFromDb(int id)
        {
            string sql = "DELETE FROM tasks WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}