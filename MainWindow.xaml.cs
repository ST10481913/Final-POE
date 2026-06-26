using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace POE2P
{
    public partial class MainWindow : Window
    {
        private ChatBot _bot;
        private TaskManager _taskManager;
        private QuizEngine _quizEngine;
        private ActivityLog _log;

        public MainWindow()
        {
            InitializeComponent();

            _log = new ActivityLog();
            _bot = new ChatBot(_log);
            _taskManager = new TaskManager(_log);
            _quizEngine = new QuizEngine(_log);

            AppendChat("Bot", "Welcome to the Cybersecurity Awareness Bot! I'm here to help you stay safe online.");
            AppendChat("Bot", "You can ask me about passwords, phishing, scams, or privacy.");
            AppendChat("Bot", "Type 'help' to see all available commands, or explore the tabs above.");

            RefreshTaskList();
            RefreshLog(false);
        }

        // ─── CHAT TAB ────────────────────────────────────────────────

        private void btnSend_Click(object sender, RoutedEventArgs e) => ProcessInput();
        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ProcessInput();
        }

        private void ProcessInput()
        {
            string input = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            AppendChat("You", input);
            txtInput.Clear();

            // NLP-based routing
            string lower = input.ToLower();

            if (NlpHelper.IsTaskAddIntent(lower))
            {
                string taskTitle = NlpHelper.ExtractTaskTitle(input);
                int days = NlpHelper.ExtractDays(lower);

                string desc = _bot.GenerateTaskDescription(taskTitle);
                _taskManager.AddTask(taskTitle, desc, days);
                RefreshTaskList();

                string reply = $"Task added: '{taskTitle}'.";
                if (days > 0)
                    reply += $" Reminder set for {days} day(s) from now ({DateTime.Now.AddDays(days):dd MMM yyyy}).";
                else
                    reply += " Would you like to set a reminder for this task?";

                AppendChat("Bot", reply);
                return;
            }

            if (NlpHelper.IsReminderIntent(lower))
            {
                int days = NlpHelper.ExtractDays(lower);
                string subject = NlpHelper.ExtractReminderSubject(input);
                DateTime target = days > 0 ? DateTime.Now.AddDays(days) : DateTime.Now.AddDays(1);

                _log.Add($"Reminder set: '{subject}' on {target:dd MMM yyyy}");
                AppendChat("Bot", $"Reminder set for '{subject}' on {target:dd MMM yyyy}.");
                RefreshLog(false);
                return;
            }

            if (NlpHelper.IsQuizIntent(lower))
            {
                AppendChat("Bot", "Head over to the Quiz tab to test your cybersecurity knowledge!");
                return;
            }

            if (NlpHelper.IsLogIntent(lower))
            {
                var recent = _log.GetRecent(10);
                if (recent.Count == 0)
                {
                    AppendChat("Bot", "No actions have been recorded yet.");
                }
                else
                {
                    AppendChat("Bot", "Here's a summary of recent actions:");
                    for (int i = 0; i < recent.Count; i++)
                        AppendChat("   ", $"{i + 1}. {recent[i]}");
                }
                return;
            }

            if (NlpHelper.IsTaskListIntent(lower))
            {
                var tasks = _taskManager.GetAll();
                if (tasks.Count == 0)
                {
                    AppendChat("Bot", "You have no tasks yet. Add one in the Tasks tab or type 'add task [name]'.");
                }
                else
                {
                    AppendChat("Bot", "Your current tasks:");
                    foreach (var t in tasks)
                        AppendChat("   ", $"- [{(t.IsComplete ? "DONE" : "    ")}] {t.Title}");
                }
                return;
            }

            // General chatbot response
            string response = _bot.GetResponse(input);
            AppendChat("Bot", response);
            RefreshLog(false);
        }

        private void AppendChat(string sender, string message)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(0, 2, 0, 2);

            Run labelRun = new Run(sender == "You" ? "You:  " : sender == "Bot" ? "Bot:  " : "      ");
            labelRun.Foreground = sender == "You"
                ? new SolidColorBrush(Color.FromRgb(0, 200, 255))
                : sender == "Bot"
                    ? new SolidColorBrush(Color.FromRgb(0, 255, 100))
                    : Brushes.Transparent;
            labelRun.FontWeight = FontWeights.Bold;

            Run msgRun = new Run(message);
            msgRun.Foreground = sender == "You"
                ? new SolidColorBrush(Color.FromRgb(180, 240, 255))
                : new SolidColorBrush(Color.FromRgb(180, 255, 180));

            para.Inlines.Add(labelRun);
            para.Inlines.Add(msgRun);
            rtbChat.Document.Blocks.Add(para);
            rtbChat.ScrollToEnd();
        }

        private void btnVoice_Click(object sender, RoutedEventArgs e)
        {
            VoicePlayer.PlayGreeting();
        }

        // ─── TASK TAB ────────────────────────────────────────────────

        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = txtTaskTitle.Text.Trim();
            string desc = txtTaskDesc.Text.Trim();
            string daysText = txtReminderDays.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a task title.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(desc))
                desc = _bot.GenerateTaskDescription(title);

            int days = 0;
            if (!string.IsNullOrEmpty(daysText) && !int.TryParse(daysText, out days))
            {
                MessageBox.Show("Reminder days must be a number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _taskManager.AddTask(title, desc, days);
            RefreshTaskList();

            txtTaskTitle.Clear();
            txtTaskDesc.Clear();
            txtReminderDays.Clear();

            AppendChat("Bot", $"Task '{title}' has been added successfully!");
            RefreshLog(false);
        }

        private void btnMarkComplete_Click(object sender, RoutedEventArgs e)
        {
            if (lstTasks.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a task to mark as complete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int idx = lstTasks.SelectedIndex;
            _taskManager.MarkComplete(idx);
            RefreshTaskList();
            AppendChat("Bot", "Task marked as complete. Well done for staying cyber-safe!");
            RefreshLog(false);
        }

        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (lstTasks.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a task to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this task?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                int idx = lstTasks.SelectedIndex;
                _taskManager.DeleteTask(idx);
                RefreshTaskList();
                pnlTaskDetail.Visibility = Visibility.Collapsed;
                RefreshLog(false);
            }
        }

        private void lstTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = lstTasks.SelectedIndex;
            if (idx < 0)
            {
                pnlTaskDetail.Visibility = Visibility.Collapsed;
                return;
            }

            var task = _taskManager.GetAll()[idx];
            string detail = $"Title:   {task.Title}\n" +
                            $"Status:  {(task.IsComplete ? "Completed ✔" : "Pending")}\n" +
                            $"Added:   {task.DateAdded:dd MMM yyyy HH:mm}\n";

            if (task.ReminderDate.HasValue)
                detail += $"Reminder: {task.ReminderDate.Value:dd MMM yyyy}";
            else
                detail += "Reminder: None";

            if (!string.IsNullOrEmpty(task.Description))
                detail += $"\n\n{task.Description}";

            lblTaskDetail.Text = detail;
            pnlTaskDetail.Visibility = Visibility.Visible;
        }

        private void RefreshTaskList()
        {
            lstTasks.Items.Clear();
            foreach (var t in _taskManager.GetAll())
            {
                string prefix = t.IsComplete ? "[✔] " : "[ ] ";
                string reminder = t.ReminderDate.HasValue
                    ? $"  ⏰ {t.ReminderDate.Value:dd MMM yyyy}"
                    : "";
                lstTasks.Items.Add(prefix + t.Title + reminder);
            }
        }

        // ─── QUIZ TAB ────────────────────────────────────────────────

        private void btnStartQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quizEngine.StartQuiz();
            btnSubmitAnswer.IsEnabled = true;
            pnlFeedback.Visibility = Visibility.Collapsed;
            AppendChat("Bot", "Quiz started! Good luck — answer wisely and learn something new.");
            RefreshLog(false);
            ShowCurrentQuestion();
        }

        private void btnSubmitAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (!_quizEngine.IsRunning)
            {
                MessageBox.Show("Please start the quiz first.", "Quiz", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selected = GetSelectedAnswer();
            if (selected < 0)
            {
                MessageBox.Show("Please select an answer.", "Quiz", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _quizEngine.SubmitAnswer(selected);
            ShowFeedback(result.IsCorrect, result.Explanation);
            UpdateScoreDisplay();

            if (_quizEngine.IsFinished)
            {
                btnSubmitAnswer.IsEnabled = false;
                string finalMsg = _quizEngine.GetFinalFeedback();
                AppendChat("Bot", finalMsg);
                lblQuestion.Text = "Quiz complete! " + finalMsg;
                RefreshLog(false);
            }
            else
            {
                ShowCurrentQuestion();
            }
        }

        private void ShowCurrentQuestion()
        {
            var q = _quizEngine.CurrentQuestion;
            if (q == null) return;

            lblQuestion.Text = q.Text;
            lblQuizProgress.Text = $"Question {_quizEngine.CurrentIndex + 1} of {_quizEngine.TotalCount}";

            var options = q.Options;
            string[] labels = { "A) ", "B) ", "C) ", "D) " };
            RadioButton[] rdos = { rdoA, rdoB, rdoC, rdoD };

            for (int i = 0; i < rdos.Length; i++)
            {
                if (i < options.Count)
                {
                    rdos[i].Content = labels[i] + options[i];
                    rdos[i].Visibility = Visibility.Visible;
                    rdos[i].IsChecked = false;
                }
                else
                {
                    rdos[i].Visibility = Visibility.Collapsed;
                }
            }

            pnlFeedback.Visibility = Visibility.Collapsed;
        }

        private void ShowFeedback(bool correct, string explanation)
        {
            lblFeedback.Text = (correct ? "✔ Correct! " : "✘ Incorrect. ") + explanation;
            lblFeedback.Foreground = correct
                ? new SolidColorBrush(Color.FromRgb(100, 255, 100))
                : new SolidColorBrush(Color.FromRgb(255, 100, 100));
            pnlFeedback.Visibility = Visibility.Visible;
        }

        private int GetSelectedAnswer()
        {
            RadioButton[] rdos = { rdoA, rdoB, rdoC, rdoD };
            for (int i = 0; i < rdos.Length; i++)
                if (rdos[i].IsChecked == true) return i;
            return -1;
        }

        private void UpdateScoreDisplay()
        {
            lblScore.Text = _quizEngine.Score.ToString();
            lblScoreTotal.Text = $" / {_quizEngine.Answered}";
        }

        // ─── LOG TAB ────────────────────────────────────────────────

        private void btnRefreshLog_Click(object sender, RoutedEventArgs e) => RefreshLog(false);
        private void btnShowFullLog_Click(object sender, RoutedEventArgs e) => RefreshLog(true);

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear the entire activity log?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _log.Clear();
                RefreshLog(false);
            }
        }

        private void RefreshLog(bool showAll)
        {
            lstActivityLog.Items.Clear();
            var entries = showAll ? _log.GetAll() : _log.GetRecent(10);

            for (int i = entries.Count - 1; i >= 0; i--)
                lstActivityLog.Items.Add($"{entries.Count - i}. {entries[i]}");

            lblLogCount.Text = showAll
                ? $"Showing all {_log.GetAll().Count} entries"
                : $"Showing last {Math.Min(10, _log.GetAll().Count)} entries";
        }
    }
}