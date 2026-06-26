using System;
using System.Collections.Generic;

namespace POE2P
{
    /// <summary>
    /// Core chatbot with keyword-based NLP and context memory.
    /// Enhanced from Part 1 and Part 2 to support task descriptions and broader topic coverage.
    /// </summary>
    public class ChatBot
    {
        private readonly Dictionary<string, List<string>> _responses;
        private readonly Random _random;
        private readonly UserMemory _memory;
        private readonly ActivityLog _log;

        public ChatBot(ActivityLog log)
        {
            _log = log;
            _random = new Random();
            _memory = new UserMemory();
            _responses = new Dictionary<string, List<string>>();

            // ── Passwords ──────────────────────────────────────────
            _responses["password"] = new List<string>
            {
                "Use strong passwords that contain uppercase letters, numbers, and special characters.",
                "Never reuse the same password across multiple accounts.",
                "Consider using a reputable password manager to store credentials securely.",
                "Change your passwords every 90 days, especially for sensitive accounts.",
                "Avoid using easily guessable information like birthdays in passwords."
            };

            // ── Phishing ───────────────────────────────────────────
            _responses["phishing"] = new List<string>
            {
                "Never click links in unsolicited emails. Go directly to the website instead.",
                "Phishing emails often use urgency — slow down and verify before acting.",
                "Check the sender's actual email address, not just the display name.",
                "Hover over links to preview the URL before clicking.",
                "When in doubt, contact the organisation directly using their official number."
            };

            // ── Privacy ────────────────────────────────────────────
            _responses["privacy"] = new List<string>
            {
                "Review app permissions regularly and revoke access you no longer need.",
                "Enable two-factor authentication on all important accounts.",
                "Avoid oversharing personal details on social media.",
                "Use a VPN on public Wi-Fi networks to protect your data.",
                "Read privacy policies before signing up for new services."
            };

            // ── Scams ──────────────────────────────────────────────
            _responses["scam"] = new List<string>
            {
                "If an offer sounds too good to be true, it usually is.",
                "Scammers often impersonate banks, government agencies, or well-known brands.",
                "Never share banking details or OTPs over the phone or via email.",
                "Be sceptical of unsolicited contact, even if it appears legitimate.",
                "Report suspicious messages to your bank and local authorities."
            };

            // ── Malware ────────────────────────────────────────────
            _responses["malware"] = new List<string>
            {
                "Keep your antivirus software updated to catch the latest threats.",
                "Only download software from official, verified sources.",
                "Be cautious about USB drives from unknown sources.",
                "Ransomware can encrypt your files — always keep secure backups.",
                "Regular operating system updates patch vulnerabilities that malware exploits."
            };

            // ── 2FA / MFA ──────────────────────────────────────────
            _responses["2fa"] = new List<string>
            {
                "Two-factor authentication adds a second verification step, making breaches harder.",
                "Use an authenticator app like Google Authenticator instead of SMS-based 2FA where possible.",
                "Enable 2FA on email, banking, and social media accounts first."
            };

            _responses["two-factor"] = _responses["2fa"];
            _responses["authentication"] = _responses["2fa"];

            // ── VPN ────────────────────────────────────────────────
            _responses["vpn"] = new List<string>
            {
                "A VPN encrypts your internet traffic, making it harder for attackers to intercept.",
                "Always use a VPN when connecting to public Wi-Fi networks.",
                "Choose a reputable VPN provider with a strict no-logs policy."
            };

            // ── Browsing ───────────────────────────────────────────
            _responses["browsing"] = new List<string>
            {
                "Always look for HTTPS in the URL before entering sensitive information.",
                "Keep your browser and its extensions updated to reduce vulnerabilities.",
                "Be cautious of pop-up warnings telling you to install software."
            };

            // ── Social engineering ─────────────────────────────────
            _responses["social engineering"] = new List<string>
            {
                "Social engineering exploits human psychology rather than technical flaws.",
                "Always verify the identity of someone requesting sensitive information.",
                "Be wary of unsolicited calls claiming to be from IT support."
            };

            // ── Help ───────────────────────────────────────────────
            _responses["help"] = new List<string>
            {
                "Available commands:\n" +
                "• Ask about: password, phishing, scam, privacy, malware, 2FA, VPN, browsing\n" +
                "• 'add task [title]' — add a cybersecurity task\n" +
                "• 'remind me to [action] in X days' — set a reminder\n" +
                "• 'show my tasks' — list current tasks\n" +
                "• 'show activity log' — view recent bot actions\n" +
                "• 'start quiz' — take the cybersecurity quiz\n" +
                "• 'tell me more' — get another tip on the same topic"
            };
        }

        public string GetResponse(string input)
        {
            string lower = input.ToLower();

            // ── Sentiment detection ────────────────────────────────
            if (lower.Contains("worried") || lower.Contains("scared") || lower.Contains("anxious"))
            {
                _log.Add("NLP: Detected user concern — provided reassurance");
                return "It is completely understandable to feel that way. Cyber threats are real, but staying informed is your best defence. I'm here to help.";
            }

            if (lower.Contains("frustrated") || lower.Contains("confused"))
            {
                _log.Add("NLP: Detected user frustration — simplified guidance offered");
                return "Let's slow down. Tell me which topic is giving you trouble and I'll break it down simply for you.";
            }

            // ── Follow-up ──────────────────────────────────────────
            if (lower.Contains("tell me more") || lower.Contains("another tip") || lower.Contains("more info"))
            {
                string topic = _memory.LastTopic ?? "general";
                _log.Add($"NLP: Follow-up request on '{topic}'");
                return GetTopicResponse(topic) ??
                       "Cybercriminals often exploit trust and urgency. Always pause before acting on unexpected requests.";
            }

            // ── Memory-driven topic check ──────────────────────────
            if (lower.Contains("privacy"))
                _memory.LastTopic = "privacy";
            else if (lower.Contains("password"))
                _memory.LastTopic = "password";
            else if (lower.Contains("phishing"))
                _memory.LastTopic = "phishing";

            // ── Keyword matching (NLP simulation) ─────────────────
            foreach (var entry in _responses)
            {
                if (lower.Contains(entry.Key))
                {
                    int pick = _random.Next(entry.Value.Count);
                    string reply = entry.Value[pick];

                    if (_memory.LastTopic != null && entry.Key != _memory.LastTopic)
                        reply += $"\n\nTip: Also review your {_memory.LastTopic} settings regularly.";

                    _log.Add($"NLP: Responded to keyword '{entry.Key}'");
                    return reply;
                }
            }

            // ── Fallback ───────────────────────────────────────────
            _log.Add("NLP: Input not recognised — fallback response given");
            return "I didn't quite catch that. Try asking about passwords, phishing, scams, privacy, or type 'help' for all options.";
        }

        /// <summary>
        /// Generates a meaningful task description based on the task title.
        /// </summary>
        public string GenerateTaskDescription(string title)
        {
            string lower = title.ToLower();

            if (lower.Contains("password"))
                return "Update your passwords to strong, unique combinations and store them securely in a password manager.";
            if (lower.Contains("2fa") || lower.Contains("two-factor") || lower.Contains("authentication"))
                return "Enable two-factor authentication to add an extra layer of protection to your accounts.";
            if (lower.Contains("privacy"))
                return "Review your privacy settings across all platforms to control what information is shared publicly.";
            if (lower.Contains("antivirus") || lower.Contains("malware"))
                return "Ensure your antivirus software is installed, active, and updated to the latest definitions.";
            if (lower.Contains("backup"))
                return "Create secure backups of important files to protect against data loss or ransomware attacks.";
            if (lower.Contains("vpn"))
                return "Set up a trusted VPN to encrypt your internet connection, especially on public networks.";
            if (lower.Contains("update") || lower.Contains("patch"))
                return "Install the latest software and operating system updates to patch known security vulnerabilities.";

            return $"Complete the task: {title}. Staying on top of cybersecurity tasks helps protect your digital life.";
        }

        private string GetTopicResponse(string topic)
        {
            if (_responses.ContainsKey(topic))
            {
                int pick = _random.Next(_responses[topic].Count);
                return _responses[topic][pick];
            }
            return null;
        }
    }
}

