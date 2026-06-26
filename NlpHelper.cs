using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace POE2P
{
    // ═════════════════════════════════════════════════════════════
    //  NLP Helper — keyword and intent detection
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Simulates Natural Language Processing using keyword detection
    /// and basic string manipulation to understand user intent.
    /// </summary>
    public static class NlpHelper
    {
        // Phrases that signal a task-add intent
        private static readonly string[] TaskAddPhrases =
        {
            "add task", "create task", "new task", "add a task",
            "add reminder", "set a task", "task to", "remind me to",
            "i need to", "i should", "set up", "enable 2fa",
            "enable two-factor", "update password", "review privacy"
        };

        // Phrases that signal a reminder-set intent
        private static readonly string[] ReminderPhrases =
        {
            "remind me", "set a reminder", "reminder for",
            "don't let me forget", "remember to", "schedule a reminder"
        };

        // Phrases that signal quiz intent
        private static readonly string[] QuizPhrases =
        {
            "start quiz", "take the quiz", "play quiz",
            "quiz me", "test my knowledge", "start the quiz", "open quiz"
        };

        // Phrases that signal log-view intent
        private static readonly string[] LogPhrases =
        {
            "show activity log", "show log", "what have you done",
            "activity log", "recent actions", "show history",
            "what did you do", "log"
        };

        // Phrases that signal task-list intent
        private static readonly string[] TaskListPhrases =
        {
            "show tasks", "my tasks", "list tasks", "view tasks",
            "show my tasks", "what tasks", "all tasks"
        };

        public static bool IsTaskAddIntent(string lower)
        {
            foreach (var phrase in TaskAddPhrases)
                if (lower.Contains(phrase)) return true;
            return false;
        }

        public static bool IsReminderIntent(string lower)
        {
            foreach (var phrase in ReminderPhrases)
                if (lower.Contains(phrase)) return true;
            return false;
        }

        public static bool IsQuizIntent(string lower)
        {
            foreach (var phrase in QuizPhrases)
                if (lower.Contains(phrase)) return true;
            return false;
        }

        public static bool IsLogIntent(string lower)
        {
            foreach (var phrase in LogPhrases)
                if (lower.Contains(phrase)) return true;
            return false;
        }

        public static bool IsTaskListIntent(string lower)
        {
            foreach (var phrase in TaskListPhrases)
                if (lower.Contains(phrase)) return true;
            return false;
        }

        /// <summary>
        /// Extracts a number of days from natural-language input.
        /// Handles patterns like "in 3 days", "in a week", "tomorrow".
        /// </summary>
        public static int ExtractDays(string lower)
        {
            if (lower.Contains("tomorrow")) return 1;
            if (lower.Contains("next week") || lower.Contains("in a week")) return 7;
            if (lower.Contains("next month")) return 30;

            // "in X days" or "in X day"
            var match = Regex.Match(lower, @"in\s+(\d+)\s+days?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                return days;

            // Just a raw number followed by "days"
            match = Regex.Match(lower, @"(\d+)\s+days?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int d))
                return d;

            return 0;
        }

        /// <summary>
        /// Attempts to extract what the task is about from the user's input.
        /// </summary>
        public static string ExtractTaskTitle(string input)
        {
            string lower = input.ToLower();

            // Try to strip the intent phrase and return the remainder
            string[] markers = { "add task", "create task", "new task", "task to", "task:", "add a task to" };
            foreach (var m in markers)
            {
                int idx = lower.IndexOf(m);
                if (idx >= 0)
                {
                    string remainder = input.Substring(idx + m.Length).Trim();
                    if (!string.IsNullOrEmpty(remainder))
                        return CapitaliseFirst(remainder);
                }
            }

            // Fallback: return everything after "to" if present, else the full input
            int toIdx = lower.IndexOf(" to ");
            if (toIdx >= 0)
                return CapitaliseFirst(input.Substring(toIdx + 4).Trim());

            return CapitaliseFirst(input.Trim());
        }

        /// <summary>
        /// Extracts the subject of a reminder request.
        /// </summary>
        public static string ExtractReminderSubject(string input)
        {
            string lower = input.ToLower();

            // Strip reminder prefix
            string[] prefixes = { "remind me to", "remind me", "set a reminder to", "set a reminder for", "remember to" };
            foreach (var p in prefixes)
            {
                int idx = lower.IndexOf(p);
                if (idx >= 0)
                {
                    string remainder = input.Substring(idx + p.Length).Trim();
                    // Also strip time references at the end
                    remainder = Regex.Replace(remainder, @"\s*(in\s+\d+\s+days?|tomorrow|next\s+week|next\s+month)\s*$", "", RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrEmpty(remainder))
                        return CapitaliseFirst(remainder);
                }
            }

            return CapitaliseFirst(input.Trim());
        }

        private static string CapitaliseFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Activity Log
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Records all significant bot actions with timestamps.
    /// Provides retrieval for the full history or a recent slice.
    /// </summary>
    public class ActivityLog
    {
        private readonly List<string> _entries;

        public ActivityLog()
        {
            _entries = new List<string>();
        }

        public void Add(string description)
        {
            string entry = $"[{DateTime.Now:dd MMM yyyy HH:mm:ss}] {description}";
            _entries.Add(entry);
        }

        /// <summary>Returns the most recent N entries.</summary>
        public List<string> GetRecent(int count)
        {
            int start = Math.Max(0, _entries.Count - count);
            return _entries.GetRange(start, _entries.Count - start);
        }

        /// <summary>Returns all entries.</summary>
        public List<string> GetAll() => new List<string>(_entries);

        public void Clear() => _entries.Clear();
    }

    // ═════════════════════════════════════════════════════════════
    //  User Memory
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Persists user context across conversation turns.
    /// Allows the bot to personalise responses based on previous topics.
    /// </summary>
    public class UserMemory
    {
        public string LastTopic { get; set; }
        public string Username { get; set; }
    }

    // ═════════════════════════════════════════════════════════════
    //  Voice Player (unchanged from Part 1/2)
    // ═════════════════════════════════════════════════════════════

    
        
    
}