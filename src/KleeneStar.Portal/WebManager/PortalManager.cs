using KleeneStar.Portal.WebDomain;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WebExpress.WebCore;
using WebExpress.WebCore.WebComponent;

namespace KleeneStar.Portal.WebManager
{
    /// <summary>
    /// Default <see cref="IPortalManager"/> implementation. The current build uses an
    /// in-memory seed that mirrors the prototype's <c>portal-data.jsx</c> so the portal
    /// looks and behaves like the design from day one. Subsequent iterations replace the
    /// seed by composing <c>CoreHub.ObjectManager</c>, <c>CoreHub.ClassManager</c>,
    /// <c>CoreHub.FormManager</c>, and <c>CoreHub.IdentityManager</c> behind the same
    /// interface — without touching pages, fragments, or hub callers.
    /// </summary>
    public sealed class PortalManager : IPortalManager
    {
        private readonly IComponentHub _componentHub;
        private readonly IHttpServerContext _httpServerContext;
        private readonly object _gate = new();
        private readonly Dictionary<string, IssueParticipant> _members;
        private readonly List<IRequestType> _requestTypes;
        private readonly Dictionary<string, Issue> _issues;
        private readonly HashSet<string> _watching;
        private int _nextIncidentNumber = 2042;

        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueCreated;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueUpdated;
        /// <inheritdoc/>
        public event EventHandler<IssueComment> IssueCommented;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueShared;
        /// <inheritdoc/>
        // Raised once the manager is wired to operator-side share-revocation events; part of the contract today.
#pragma warning disable CS0067
        public event EventHandler<IIssue> IssueUnshared;
#pragma warning restore CS0067
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueWatched;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueUnwatched;
        /// <inheritdoc/>
        // Raised when the operator side proposes a resolution; the portal-side accept/reject events
        // are already implemented and used by AcceptResolution/RejectResolution.
#pragma warning disable CS0067
        public event EventHandler<IIssue> IssueResolutionProposed;
#pragma warning restore CS0067
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueResolutionAccepted;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueResolutionRejected;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueClosed;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub.</param>
        /// <param name="httpServerContext">The reference to the context of the host.</param>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used via Reflection.")]
        private PortalManager(IComponentHub componentHub, IHttpServerContext httpServerContext)
        {
            _componentHub = componentHub;
            _httpServerContext = httpServerContext;

            _members = SeedMembers().ToDictionary(m => m.Id);
            _requestTypes = [.. SeedRequestTypes()];
            _issues = SeedIssues(_members, _requestTypes).ToDictionary(i => i.Key);
            _watching = [.. _issues.Values
                .Where(i => i.Watchers.Any(w => w.Id == "u1"))
                .Select(i => i.Key)];
        }

        /// <inheritdoc/>
        public IssueParticipant CurrentUser => _members["u1"];

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> GetOrganizationMembers()
        {
            lock (_gate)
            {
                return [.. _members.Values];
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IRequestType> GetRequestTypes()
        {
            lock (_gate)
            {
                return [.. _requestTypes];
            }
        }

        /// <inheritdoc/>
        public IRequestType GetRequestType(string requestTypeKey)
        {
            lock (_gate)
            {
                return _requestTypes.FirstOrDefault(r => string.Equals(r.Key, requestTypeKey, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IIssue> GetIssues(IssueScope scope)
        {
            lock (_gate)
            {
                var me = CurrentUser.Id;

                bool isMine(Issue i) =>
                    i.Requester?.Id == me ||
                    i.SharedWith.Any(p => p.Id == me) ||
                    i.Watchers.Any(p => p.Id == me);

                var query = scope == IssueScope.Mine
                    ? _issues.Values.Where(isMine)
                    : _issues.Values;

                return [.. query.OrderByDescending(i => i.Updated)];
            }
        }

        /// <inheritdoc/>
        public IIssue GetIssue(string issueKey)
        {
            lock (_gate)
            {
                return _issues.TryGetValue(issueKey ?? string.Empty, out var issue) ? issue : null;
            }
        }

        /// <inheritdoc/>
        public IIssue CreateIssue(string requestTypeKey, string templateKey, string title, string description, string priority)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            lock (_gate)
            {
                var requestType = GetRequestType(requestTypeKey)
                    ?? throw new InvalidOperationException($"Request type '{requestTypeKey}' not found.");

                var key = $"INC-{_nextIncidentNumber++}";
                var now = DateTime.UtcNow;

                var issue = new Issue
                {
                    Key = key,
                    Title = title,
                    Description = description ?? string.Empty,
                    RequestType = requestType,
                    RequestTypeName = requestType.Title,
                    Priority = priority ?? "P3",
                    PortalState = PortalIssueState.Open,
                    Requester = CurrentUser,
                    AssigneeLabel = "Service Desk · Tier 1",
                    Created = now,
                    Updated = now,
                    SharedWith = [],
                    Watchers = [CurrentUser],
                    Comments = []
                };

                _issues[key] = issue;
                _watching.Add(key);

                IssueCreated?.Invoke(this, issue);
                return issue;
            }
        }

        /// <inheritdoc/>
        public IIssue AddComment(string issueKey, string text, string visibility)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            lock (_gate)
            {
                if (!_issues.TryGetValue(issueKey ?? string.Empty, out var issue))
                {
                    return null;
                }

                var comment = new IssueComment
                {
                    Id = "c" + Guid.NewGuid().ToString("N")[..8],
                    Author = CurrentUser,
                    Role = "Anfragesteller",
                    Visibility = visibility ?? "public",
                    Timestamp = DateTime.UtcNow,
                    Text = text
                };

                var updated = WithComments(issue, [.. issue.Comments, comment], DateTime.UtcNow);
                _issues[issueKey] = updated;

                IssueCommented?.Invoke(this, comment);
                IssueUpdated?.Invoke(this, updated);
                return updated;
            }
        }

        /// <inheritdoc/>
        public IIssue ShareIssue(string issueKey, IEnumerable<string> identityIds)
        {
            ArgumentNullException.ThrowIfNull(identityIds);

            lock (_gate)
            {
                if (!_issues.TryGetValue(issueKey ?? string.Empty, out var issue))
                {
                    return null;
                }

                var added = identityIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Where(id => !issue.SharedWith.Any(p => p.Id == id))
                    .Select(id => _members.TryGetValue(id, out var m) ? m : null)
                    .Where(m => m is not null)
                    .ToList();

                if (added.Count == 0)
                {
                    return issue;
                }

                var shared = issue.SharedWith.Concat(added).ToList();
                var note = SystemNote($"{CurrentUser.Name} hat den Vorgang mit {added.Count} weitere"
                    + (added.Count == 1 ? "r Person" : "n Personen")
                    + " geteilt.");
                var updated = new Issue
                {
                    Key = issue.Key,
                    Title = issue.Title,
                    Description = issue.Description,
                    RequestType = issue.RequestType,
                    RequestTypeName = issue.RequestTypeName,
                    Priority = issue.Priority,
                    PortalState = issue.PortalState,
                    Requester = issue.Requester,
                    AssigneeLabel = issue.AssigneeLabel,
                    RequiresApproval = issue.RequiresApproval,
                    Created = issue.Created,
                    Updated = DateTime.UtcNow,
                    SharedWith = shared,
                    Watchers = issue.Watchers,
                    Comments = [.. issue.Comments, note]
                };
                _issues[issueKey] = updated;

                IssueShared?.Invoke(this, updated);
                IssueUpdated?.Invoke(this, updated);
                return updated;
            }
        }

        /// <inheritdoc/>
        public IIssue Watch(string issueKey)
        {
            return SetWatching(issueKey, true);
        }

        /// <inheritdoc/>
        public IIssue Unwatch(string issueKey)
        {
            return SetWatching(issueKey, false);
        }

        /// <inheritdoc/>
        public IIssue AcceptResolution(string issueKey)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueKey ?? string.Empty, out var issue))
                {
                    return null;
                }
                if (issue.PortalState != PortalIssueState.Resolved)
                {
                    return issue;
                }

                var note = SystemNote($"{CurrentUser.Name} hat die Lösung akzeptiert. Vorgang geschlossen.");
                var updated = new Issue
                {
                    Key = issue.Key,
                    Title = issue.Title,
                    Description = issue.Description,
                    RequestType = issue.RequestType,
                    RequestTypeName = issue.RequestTypeName,
                    Priority = issue.Priority,
                    PortalState = PortalIssueState.Closed,
                    Requester = issue.Requester,
                    AssigneeLabel = issue.AssigneeLabel,
                    RequiresApproval = issue.RequiresApproval,
                    Created = issue.Created,
                    Updated = DateTime.UtcNow,
                    SharedWith = issue.SharedWith,
                    Watchers = issue.Watchers,
                    Comments = [.. issue.Comments, note]
                };
                _issues[issueKey] = updated;

                IssueResolutionAccepted?.Invoke(this, updated);
                IssueClosed?.Invoke(this, updated);
                IssueUpdated?.Invoke(this, updated);
                return updated;
            }
        }

        /// <inheritdoc/>
        public IIssue RejectResolution(string issueKey, string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            lock (_gate)
            {
                if (!_issues.TryGetValue(issueKey ?? string.Empty, out var issue))
                {
                    return null;
                }
                if (issue.PortalState != PortalIssueState.Resolved)
                {
                    return issue;
                }

                var note = SystemNote($"{CurrentUser.Name} hat die vorgeschlagene Lösung abgelehnt: {reason}");
                var updated = new Issue
                {
                    Key = issue.Key,
                    Title = issue.Title,
                    Description = issue.Description,
                    RequestType = issue.RequestType,
                    RequestTypeName = issue.RequestTypeName,
                    Priority = issue.Priority,
                    PortalState = PortalIssueState.InProgress,
                    Requester = issue.Requester,
                    AssigneeLabel = issue.AssigneeLabel,
                    RequiresApproval = issue.RequiresApproval,
                    Created = issue.Created,
                    Updated = DateTime.UtcNow,
                    SharedWith = issue.SharedWith,
                    Watchers = issue.Watchers,
                    Comments = [.. issue.Comments, note]
                };
                _issues[issueKey] = updated;

                IssueResolutionRejected?.Invoke(this, updated);
                IssueUpdated?.Invoke(this, updated);
                return updated;
            }
        }

        /// <summary>
        /// Release of unmanaged resources reserved during use.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private IIssue SetWatching(string issueKey, bool watch)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueKey ?? string.Empty, out var issue))
                {
                    return null;
                }

                var watchers = issue.Watchers.Where(w => w.Id != CurrentUser.Id).ToList();
                if (watch)
                {
                    watchers.Add(CurrentUser);
                    _watching.Add(issueKey);
                }
                else
                {
                    _watching.Remove(issueKey);
                }

                var note = SystemNote(watch
                    ? $"{CurrentUser.Name} beobachtet diesen Vorgang."
                    : $"{CurrentUser.Name} beobachtet diesen Vorgang nicht mehr.");

                var updated = new Issue
                {
                    Key = issue.Key,
                    Title = issue.Title,
                    Description = issue.Description,
                    RequestType = issue.RequestType,
                    RequestTypeName = issue.RequestTypeName,
                    Priority = issue.Priority,
                    PortalState = issue.PortalState,
                    Requester = issue.Requester,
                    AssigneeLabel = issue.AssigneeLabel,
                    RequiresApproval = issue.RequiresApproval,
                    Created = issue.Created,
                    Updated = DateTime.UtcNow,
                    SharedWith = issue.SharedWith,
                    Watchers = watchers,
                    Comments = [.. issue.Comments, note]
                };
                _issues[issueKey] = updated;

                if (watch) { IssueWatched?.Invoke(this, updated); }
                else { IssueUnwatched?.Invoke(this, updated); }
                IssueUpdated?.Invoke(this, updated);

                return updated;
            }
        }

        private static Issue WithComments(Issue source, IReadOnlyList<IssueComment> comments, DateTime updated)
        {
            return new Issue
            {
                Key = source.Key,
                Title = source.Title,
                Description = source.Description,
                RequestType = source.RequestType,
                RequestTypeName = source.RequestTypeName,
                Priority = source.Priority,
                PortalState = source.PortalState,
                Requester = source.Requester,
                AssigneeLabel = source.AssigneeLabel,
                RequiresApproval = source.RequiresApproval,
                Created = source.Created,
                Updated = updated,
                SharedWith = source.SharedWith,
                Watchers = source.Watchers,
                Comments = comments
            };
        }

        private static IssueComment SystemNote(string text)
        {
            return new IssueComment
            {
                Id = "c" + Guid.NewGuid().ToString("N")[..8],
                Author = null,
                IsSystem = true,
                Role = "system",
                Visibility = "public",
                Timestamp = DateTime.UtcNow,
                Text = text
            };
        }

        private static IEnumerable<IssueParticipant> SeedMembers()
        {
            yield return new IssueParticipant { Id = "u1", Name = "Anna Becker", Email = "anna.becker@acme.de" };
            yield return new IssueParticipant { Id = "u2", Name = "Markus Schmidt", Email = "markus.schmidt@acme.de" };
            yield return new IssueParticipant { Id = "u3", Name = "Lara Petrov", Email = "lara.petrov@acme.de" };
            yield return new IssueParticipant { Id = "u4", Name = "Tomás Rivera", Email = "tomas.rivera@acme.de" };
            yield return new IssueParticipant { Id = "u5", Name = "Sina Köhler", Email = "sina.koehler@acme.de" };
        }

        private static IEnumerable<IRequestType> SeedRequestTypes()
        {
            yield return new RequestType
            {
                Key = "incident",
                Title = "Störung melden",
                Description = "Ein Service ist nicht erreichbar oder fehlerhaft.",
                IconKey = "lightning",
                Tone = "oklch(96% 0.05 25)",
                Foreground = "oklch(50% 0.18 25)",
                Templates =
                [
                    new Template { Key = "tpl-mail", Title = "E-Mail funktioniert nicht", Description = "Senden oder Empfangen blockiert." },
                    new Template { Key = "tpl-vpn",  Title = "VPN trennt die Verbindung", Description = "Verbindung bricht regelmäßig ab." },
                    new Template { Key = "tpl-app",  Title = "Anwendung stürzt ab",       Description = "Software hängt sich auf." }
                ]
            };
            yield return new RequestType
            {
                Key = "access",
                Title = "Neuen Zugang anfordern",
                Description = "Software-Lizenzen, System-Accounts, Zugangskarten.",
                IconKey = "plus",
                Tone = "oklch(96% 0.05 252)",
                Foreground = "oklch(48% 0.18 252)",
                Templates =
                [
                    new Template { Key = "tpl-o365",  Title = "Office 365 Lizenz", Description = "Standard-Paket inkl. Outlook und Teams." },
                    new Template { Key = "tpl-vpn-acc", Title = "VPN-Zugang",      Description = "Remote-Zugang für externe Standorte." },
                    new Template { Key = "tpl-bldg",  Title = "Gebäudezugang",     Description = "Schlüsselkarten und Zonen-Freigabe." }
                ]
            };
            yield return new RequestType
            {
                Key = "service",
                Title = "Service-Anfrage",
                Description = "Konfigurationen, Anpassungen oder Hilfe bei Standard-Aufgaben.",
                IconKey = "cog",
                Tone = "oklch(96% 0.04 75)",
                Foreground = "oklch(50% 0.16 75)",
                Templates =
                [
                    new Template { Key = "tpl-printer", Title = "Drucker einrichten",     Description = "Neuer Drucker oder Treiber-Update." },
                    new Template { Key = "tpl-restore", Title = "Daten wiederherstellen", Description = "Aus Backup zurückspielen." }
                ]
            };
            yield return new RequestType
            {
                Key = "advice",
                Title = "Frage / Beratung",
                Description = "Allgemeine Fragen ans Service-Team.",
                IconKey = "info",
                Tone = "oklch(96% 0.04 155)",
                Foreground = "oklch(48% 0.14 155)",
                Templates =
                [
                    new Template { Key = "tpl-howto", Title = "How-to: Tool XY benutzen", Description = "Anleitung anfordern." },
                    new Template { Key = "tpl-best",  Title = "Best-practice Beratung",   Description = "Empfehlung holen." }
                ]
            };
            yield return new RequestType
            {
                Key = "document",
                Title = "Dokument / Vertrag",
                Description = "Verträge, Bestellungen, formale Dokumente.",
                IconKey = "file",
                Tone = "oklch(96% 0.04 285)",
                Foreground = "oklch(48% 0.16 285)",
                Templates =
                [
                    new Template { Key = "tpl-order",   Title = "Bestellanfrage",       Description = "Hardware oder Software bestellen." },
                    new Template { Key = "tpl-renew",   Title = "Vertragsverlängerung", Description = "Bestehenden Vertrag verlängern." }
                ]
            };
            yield return new RequestType
            {
                Key = "feedback",
                Title = "Feedback / Ideen",
                Description = "Verbesserungsvorschläge und Lob.",
                IconKey = "status",
                Tone = "oklch(96% 0.04 320)",
                Foreground = "oklch(48% 0.18 320)",
                Templates =
                [
                    new Template { Key = "tpl-feature", Title = "Feature-Wunsch", Description = "Eine neue Funktion vorschlagen." }
                ]
            };
        }

        private static IEnumerable<Issue> SeedIssues(IDictionary<string, IssueParticipant> members, IList<IRequestType> requestTypes)
        {
            IRequestType byKey(string key) => requestTypes.First(r => r.Key == key);

            var u1 = members["u1"];
            var u2 = members["u2"];
            var u3 = members["u3"];
            var u4 = members["u4"];

            yield return new Issue
            {
                Key = "INC-2041",
                Title = "Outlook empfängt keine externen E-Mails",
                Description = "Seit heute Morgen kommen keine E-Mails mehr von extern an. Interne Mails funktionieren normal. Outlook 2024, Win 11.",
                RequestType = byKey("incident"),
                RequestTypeName = "Störung",
                Priority = "P2",
                PortalState = PortalIssueState.InProgress,
                Requester = u1,
                AssigneeLabel = "Service Desk · Tier 2",
                Created = new DateTime(2026, 4, 30, 8, 14, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddHours(-2),
                SharedWith = [u2, u3],
                Watchers = [u1, u2],
                Comments =
                [
                    new IssueComment { Id = "c1", Author = u1, Role = "Anfragesteller", Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-3), Text = "Habe Outlook neu gestartet, ohne Erfolg." },
                    new IssueComment { Id = "c2", Author = u2, Role = "Service Desk",   Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-2), Text = "Hallo Anna, wir prüfen den Mail-Filter. Kannst du eine Beispiel-Mail weiterleiten?" },
                    new IssueComment { Id = "c3", Author = u1, Role = "Anfragesteller", Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-1), Text = "Habe ich gerade gemacht." },
                    new IssueComment { Id = "c4", Author = null, IsSystem = true, Role = "system", Timestamp = DateTime.UtcNow.AddHours(-4), Text = "Status geändert: Wartet auf Antrag → In Bearbeitung" }
                ]
            };
            yield return new Issue
            {
                Key = "REQ-1284",
                Title = "Office 365 Lizenz für neuen Mitarbeiter",
                Description = "Neuer Kollege Tomás startet am 15.05. — bitte Standard-Paket vorbereiten.",
                RequestType = byKey("access"),
                RequestTypeName = "Zugang",
                Priority = "P3",
                PortalState = PortalIssueState.WaitingOnRequester,
                Requester = u1,
                AssigneeLabel = "IT-Beschaffung",
                RequiresApproval = true,
                Created = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddDays(-1),
                SharedWith = [],
                Watchers = [u1],
                Comments =
                [
                    new IssueComment { Id = "c5", Author = new IssueParticipant { Id = "svc", Name = "IT-Beschaffung", Email = "it-beschaffung@acme.de" }, Role = "Service Desk", Timestamp = DateTime.UtcNow.AddDays(-1), Text = "Bitte Genehmigung der Kostenstelle bestätigen, dann lösen wir die Bestellung aus." }
                ]
            };
            yield return new Issue
            {
                Key = "INC-2038",
                Title = "VPN-Verbindung bricht alle 5 min ab",
                Description = "Im Homeoffice trennt der VPN-Client alle paar Minuten die Verbindung. Tritt seit gestern auf.",
                RequestType = byKey("incident"),
                RequestTypeName = "Störung",
                Priority = "P2",
                PortalState = PortalIssueState.InProgress,
                Requester = u2,
                AssigneeLabel = "Network Team",
                Created = new DateTime(2026, 4, 29, 9, 0, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddHours(-3),
                SharedWith = [u1],
                Watchers = [u1, u2],
                Comments =
                [
                    new IssueComment { Id = "c6", Author = u2, Role = "Anfragesteller", Timestamp = DateTime.UtcNow.AddHours(-6), Text = "Schon zweimal die Konfiguration neu importiert." }
                ]
            };
            yield return new Issue
            {
                Key = "REQ-1276",
                Title = "Gebäudezugang für externen Berater",
                Description = "Berater Petrov braucht für 2 Wochen Zugang zu Bereich C.",
                RequestType = byKey("access"),
                RequestTypeName = "Zugang",
                Priority = "P4",
                PortalState = PortalIssueState.Resolved,
                Requester = u3,
                AssigneeLabel = "Facility Management",
                Created = new DateTime(2026, 4, 21, 11, 0, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddDays(-4),
                SharedWith = [],
                Watchers = [u3],
                Comments = []
            };
            yield return new Issue
            {
                Key = "INC-2025",
                Title = "Drucker im 3. OG druckt Streifen",
                Description = "Ausdrucke haben senkrechte schwarze Streifen. Toner ist getauscht, kein Effekt.",
                RequestType = byKey("incident"),
                RequestTypeName = "Service",
                Priority = "P3",
                PortalState = PortalIssueState.Open,
                Requester = u1,
                AssigneeLabel = "Hardware Support",
                Created = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddHours(-5),
                SharedWith = [u4],
                Watchers = [u1],
                Comments = []
            };
            yield return new Issue
            {
                Key = "REQ-1268",
                Title = "Backup-Wiederherstellung 'Q1-Reports'",
                Description = "Versehentlich gelöschtes Verzeichnis aus Backup vom 10.04. herstellen.",
                RequestType = byKey("service"),
                RequestTypeName = "Service",
                Priority = "P3",
                PortalState = PortalIssueState.Closed,
                Requester = u4,
                AssigneeLabel = "Storage Team",
                Created = new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc),
                Updated = DateTime.UtcNow.AddDays(-7),
                SharedWith = [u1, u2, u3],
                Watchers = [],
                Comments = []
            };
        }
    }
}
