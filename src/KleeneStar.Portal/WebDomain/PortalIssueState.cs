namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// The portal-visible projection of the issue lifecycle. Internal operator-side states
    /// (triage, escalation, on-hold, …) collapse into <see cref="InProgress"/> so the
    /// requester always sees a coherent low-noise lifecycle.
    /// </summary>
    public enum PortalIssueState
    {
        /// <summary>
        /// The issue has been created by the requester and is waiting for the service team
        /// to start work.
        /// </summary>
        Open,

        /// <summary>
        /// The service team is actively working on the issue.
        /// </summary>
        InProgress,

        /// <summary>
        /// The service team has asked the requester for additional information, an approval,
        /// or a confirmation. The issue is paused until the requester responds.
        /// </summary>
        WaitingOnRequester,

        /// <summary>
        /// The service team considers the issue done and proposes a resolution. The requester
        /// is asked to accept or reject.
        /// </summary>
        Resolved,

        /// <summary>
        /// The issue is finalized — either by acceptance, by timeout, or administratively.
        /// </summary>
        Closed
    }
}
