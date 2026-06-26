using System;
using System.Collections.Generic;

namespace POE2P
{
    /// <summary>
    /// A single quiz question with multiple-choice options.
    /// </summary>
    public class QuizQuestion
    {
        public string Text { get; set; }
        public List<string> Options { get; set; }
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }
    }

    /// <summary>
    /// Result of a single answer submission.
    /// </summary>
    public class AnswerResult
    {
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; }
    }

    /// <summary>
    /// Manages quiz state, questions, and scoring.
    /// Covers phishing, passwords, safe browsing, malware, and social engineering.
    /// </summary>
    public class QuizEngine
    {
        private readonly List<QuizQuestion> _allQuestions;
        private List<QuizQuestion> _session;
        private readonly ActivityLog _log;
        private int _current;

        public int Score { get; private set; }
        public int Answered { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFinished => IsRunning && _current >= _session.Count;
        public int TotalCount => _session?.Count ?? 0;
        public int CurrentIndex => _current;

        public QuizQuestion CurrentQuestion =>
            (IsRunning && _current < _session.Count) ? _session[_current] : null;

        public QuizEngine(ActivityLog log)
        {
            _log = log;
            _allQuestions = BuildQuestions();
        }

        public void StartQuiz()
        {
            _session = new List<QuizQuestion>(_allQuestions);
            Shuffle(_session);
            _current = 0;
            Score = 0;
            Answered = 0;
            IsRunning = true;
            _log.Add("Quiz started");
        }

        public AnswerResult SubmitAnswer(int selectedIndex)
        {
            if (!IsRunning || _current >= _session.Count)
                return new AnswerResult { IsCorrect = false, Explanation = "Quiz is not running." };

            var q = _session[_current];
            bool correct = selectedIndex == q.CorrectIndex;

            if (correct) Score++;
            Answered++;
            _current++;

            string resultText = correct ? "correct" : $"incorrect (answer was: {q.Options[q.CorrectIndex]})";
            _log.Add($"Quiz Q{Answered}: {resultText}");

            if (IsFinished)
                _log.Add($"Quiz completed — Score: {Score}/{TotalCount}");

            return new AnswerResult
            {
                IsCorrect = correct,
                Explanation = q.Explanation
            };
        }

        public string GetFinalFeedback()
        {
            double pct = TotalCount > 0 ? (double)Score / TotalCount * 100 : 0;

            if (pct >= 90)
                return $"Outstanding! You scored {Score}/{TotalCount}. You're a true cybersecurity expert!";
            if (pct >= 70)
                return $"Great job! You scored {Score}/{TotalCount}. You have solid cybersecurity knowledge.";
            if (pct >= 50)
                return $"Not bad. You scored {Score}/{TotalCount}. Keep studying — you're getting there!";

            return $"You scored {Score}/{TotalCount}. Cybersecurity takes practice — keep learning to stay safe online!";
        }

        private List<QuizQuestion> BuildQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Text = "What should you do if you receive an unexpected email asking for your password?",
                    Options = new List<string>
                    {
                        "Reply with your password",
                        "Delete the email",
                        "Report the email as phishing",
                        "Forward it to a friend"
                    },
                    CorrectIndex = 2,
                    Explanation = "Reporting phishing emails helps protect others and alerts your email provider to the threat."
                },
                new QuizQuestion
                {
                    Text = "Which of the following is considered a strong password?",
                    Options = new List<string>
                    {
                        "password123",
                        "John1985",
                        "T!g3r$Ru@n#9",
                        "qwerty"
                    },
                    CorrectIndex = 2,
                    Explanation = "Strong passwords combine uppercase, lowercase, numbers, and special characters to make them harder to crack."
                },
                new QuizQuestion
                {
                    Text = "TRUE or FALSE: Using the same password for multiple accounts is safe if it is a strong password.",
                    Options = new List<string>
                    {
                        "True",
                        "False"
                    },
                    CorrectIndex = 1,
                    Explanation = "If one account is breached, attackers can use the same credentials to access your other accounts — a technique called credential stuffing."
                },
                new QuizQuestion
                {
                    Text = "What does HTTPS in a website URL indicate?",
                    Options = new List<string>
                    {
                        "The site is run by the government",
                        "The connection between your browser and the site is encrypted",
                        "The site is free of viruses",
                        "The site does not collect cookies"
                    },
                    CorrectIndex = 1,
                    Explanation = "HTTPS uses TLS/SSL encryption to protect data in transit, but it does not guarantee a site is safe or legitimate."
                },
                new QuizQuestion
                {
                    Text = "A colleague calls saying they are from IT support and need your login details to fix a problem. What should you do?",
                    Options = new List<string>
                    {
                        "Give them your username only",
                        "Provide all details since they are from IT",
                        "Decline and report the call to your actual IT department",
                        "Email the details instead"
                    },
                    CorrectIndex = 2,
                    Explanation = "Legitimate IT staff will never ask for your password. This is a social engineering attack called pretexting."
                },
                new QuizQuestion
                {
                    Text = "What is two-factor authentication (2FA)?",
                    Options = new List<string>
                    {
                        "Logging in from two different devices",
                        "A second verification step added after your password",
                        "Having two different passwords for one account",
                        "A type of firewall"
                    },
                    CorrectIndex = 1,
                    Explanation = "2FA requires something you know (password) AND something you have (e.g. a one-time code), making accounts much harder to compromise."
                },
                new QuizQuestion
                {
                    Text = "TRUE or FALSE: Public Wi-Fi networks are generally safe to use for banking without a VPN.",
                    Options = new List<string>
                    {
                        "True",
                        "False"
                    },
                    CorrectIndex = 1,
                    Explanation = "Public Wi-Fi is unencrypted and vulnerable to man-in-the-middle attacks. Always use a VPN or avoid sensitive activities on public networks."
                },
                new QuizQuestion
                {
                    Text = "Which of the following is a sign that an email might be a phishing attempt?",
                    Options = new List<string>
                    {
                        "It comes from a person you know",
                        "It contains urgent language and asks you to click a link",
                        "It has no attachments",
                        "It was sent during business hours"
                    },
                    CorrectIndex = 1,
                    Explanation = "Phishing emails often create a sense of urgency to pressure you into acting quickly without thinking critically."
                },
                new QuizQuestion
                {
                    Text = "What is ransomware?",
                    Options = new List<string>
                    {
                        "Software that monitors your screen",
                        "A virus that displays pop-up ads",
                        "Malware that encrypts your files and demands payment for the decryption key",
                        "A tool used by ethical hackers"
                    },
                    CorrectIndex = 2,
                    Explanation = "Ransomware encrypts your data and demands payment. Regular offline backups are your best protection against it."
                },
                new QuizQuestion
                {
                    Text = "Why is it important to keep your software and operating system updated?",
                    Options = new List<string>
                    {
                        "Updates add new visual themes",
                        "Updates speed up your internet connection",
                        "Updates patch security vulnerabilities that attackers could exploit",
                        "Updates are optional and rarely useful"
                    },
                    CorrectIndex = 2,
                    Explanation = "Software updates often contain critical security patches. Delaying them leaves known vulnerabilities open to attack."
                },
                new QuizQuestion
                {
                    Text = "What is social engineering in cybersecurity?",
                    Options = new List<string>
                    {
                        "Building secure social media platforms",
                        "Hacking through brute force password attacks",
                        "Manipulating people psychologically to reveal confidential information",
                        "Encrypting social media data"
                    },
                    CorrectIndex = 2,
                    Explanation = "Social engineering targets human behaviour rather than technical systems. Awareness and scepticism are your best defences."
                },
                new QuizQuestion
                {
                    Text = "TRUE or FALSE: Antivirus software alone is enough to protect you from all cyber threats.",
                    Options = new List<string>
                    {
                        "True",
                        "False"
                    },
                    CorrectIndex = 1,
                    Explanation = "Antivirus is one layer of protection. A complete security posture includes strong passwords, 2FA, safe browsing habits, and user awareness."
                }
            };
        }

        private void Shuffle(List<QuizQuestion> list)
        {
            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}