![KleeneStar](https://raw.githubusercontent.com/kleenestar-project/.github/main/docs/assets/img/banner.png)

# KleeneStar Customer Portal Concept

This document specifies the **Customer Portal** of the **KleeneStar** ecosystem. The portal is the public-facing counterpart to the internal **KleeneStar** WebApp: while the WebApp is the operator surface for service teams, administrators, and content modelers, the portal is the surface through which an organization's end users interact with the modeled content. End users in this context are not employees of the operator, but employees of the **tenants** (e.g., the IT-customer, the requesting business unit, an external contractor).

The portal exposes a deliberately reduced subset of **KleeneStar** to this audience. Internal classes, fields, workflows, and dashboards are not directly visible; instead, the portal presents pre-curated **request types** (e.g., "report incident", "request access"), pre-defined **templates**, and a personalized list of **issues** (a domain-specific name for objects that arose from a portal submission) the user has created, has access to, or has been invited to. The portal therefore acts as a controlled projection of the underlying object model — every interaction it offers maps to operations defined in `kleenestar.class.md`, `kleenestar.workflow.md`, `kleenestar.form.md`, and `kleenestar.object.md`, but constrained by tenant scope, identity context, and the active permissions profile.

The portal is a self-contained WebExpress application (`KleeneStar.Portal`) that runs alongside the operator WebApp inside the same host. It shares the data layer (`KleeneStarDbContext`), the manager hub (`CoreHub` / `ModelHub`), and the identity model with the WebApp, but ships its own pages, navigation shell, and theme. This separation allows the operator surface and the customer surface to evolve and to be branded independently while remaining a single source of truth for the data they share.

## Lifecycle of an Issue

The functional scope of the portal centers on the lifecycle of a **issue**, which from the data-model perspective is an `IObject` instance of a class that has been declared `Portal-Visible` (see *Permissions Model*). A issue goes through a portal-visible state machine that is a projection of the full workflow defined for its class (see `kleenestar.workflow.md`). The portal collapses internal-only states (e.g., triage, internal review) into the user-facing state `inProgress`, so the customer always sees a coherent, low-noise lifecycle.

The portal-visible states are:

- **Open**: The issue has been created by the requester and is waiting for the service team to start work.
- **In Progress**: The service team is actively working on the issue. Internal sub-states (triage, escalated, on-hold) collapse into this state from the customer's point of view.
- **Waiting on Requester**: The service team has asked the requester for additional information, an approval, or a confirmation. The issue is paused until the requester responds.
- **Resolved**: The service team considers the issue done and proposes a resolution. The requester is asked to accept or reject the resolution.
- **Closed**: The issue is finalized — either because the requester accepted the resolution, the time-to-confirm window expired, or the issue was closed administratively.

```
╔══════════════════════════════════════════════════════════════════════════════════════╗
║                       KleeneStar Portal Issue State Diagram                          ║
╠══════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                      ║
║                              ┌──────────────────────────┐                            ║
║                              │  ask requester           │                            ║
║                              │                          │                            ║
║                  ╔══════╗ pickup   ╔═════════════╗  ┌───▼────────────────┐           ║
║              new ║ open ║──────────► in progress ║──┤ waiting on         │           ║
║              ────╚══════╝          ╚═══════╤═════╝  │   requester        │           ║
║                                            │   ▲    └─────────────┬──────┘           ║
║                                            │   └──── reply ───────┘                  ║
║                                            │ propose resolution                      ║
║                                            │                                         ║
║                                       ┌────▼─────┐                                   ║
║                                       │ resolved │                                   ║
║                                       └─┬──────┬─┘                                   ║
║                                  reject │      │ accept / timeout                    ║
║                                         │      │                                     ║
║                                         │      ▼                                     ║
║                                         │  ╔════════╗                                ║
║                                         │  ║ closed ║                                ║
║                                         │  ╚════════╝                                ║
║                                         │                                            ║
║                                         └──► (back to in progress)                   ║
║                                                                                      ║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

The state diagram describes only the portal-visible projection. Operator-side transitions (e.g., escalation, splitting, internal handover) happen on the underlying `IObject` and are propagated to the portal exclusively as state changes of the projected lifecycle and as system entries in the issue history.

## Data Model

The portal does not introduce a new data root. It is a constrained view onto the existing **KleeneStar** Core data model. Each request type tile a customer sees is backed by a `Class` whose declaration enables portal visibility. Each issue is backed by an `Object` of that class. Templates are pre-bound `Form` instances that hide irrelevant fields and prefill defaults. Comments are `Comment` records, attachments are `FileReference` records, and status changes are `Version` records.

The portal additionally relies on two relationship concepts that are layered on top of the core model via `Link`:

- **Sharing**: An explicit `Link` between an `Object` (the issue) and an `Identity` (a user from the same tenant) that grants the linked identity read/comment access to the issue without making them the requester. Shared identities appear in the issue's "Shared with" panel.
- **Watching**: A subscription `Link` between an `Identity` and an `Object`. Watchers receive notifications on relevant state and comment events but do not gain additional read rights beyond what their group profile already permits.

```
╔══════════════════════════════════════════════════════════════════════════════════════╗
║                             KleeneStar Core Data Model                               ║
╠══════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                      ║
║                         ┌────────────────┬────────────────────────────────┐          ║
║                         │ *              │ *                              │          ║
║                   ┌─────▼────┐     ┌─────▼────┐      ┌──────┐ 1           │          ║
║                   │ Workflow │     │ Priority │      │ Form ├───┐         │          ║
║                   └─────┬────┘     └─────┬────┘      └───┬──┘   │         │          ║
║                         │ *              │ *             │ *    │         │          ║
║                         └────────────────┼───────────────┘      │         │          ║
║                                          │                      │         │          ║
║                                          │ 1                    │ *       │          ║
║          ┌───────────┐ *           * ┌───▼───┐ 1          * ┌───▼───┐ 0,1 │          ║
║          │ Workspace ├───────────────► Class ◄──────────────┤ Field ├─────┘          ║
║          └─────┬─────┘               └───▲───┘              └───▲───┘                ║
║                │ 1                       │ 1                    │ 1                  ║
║                └────────────────────┐    │                      │                    ║
║                                     │ *  │ *                    │ *                  ║
║    ┌───────────┐  ┌──────┐ *    2 ┌─▼────┴─┐ 1            * ┌───┴───┐                ║
║    │ Dashboard │  │ Link ├────────► Object ├────────────────► Value │                ║
║    └─────┬─────┘  └──────┘        └─▲────▲─┘                └───▲───┘                ║
║          │ 1                        │ 1  │ 1                    │ 1                  ║
║          │              ┌───────────┘    │                      │                    ║
║          │ *            │ *              │ *                    │ *                  ║
║     ┌────▼───┐     ┌────┴────┐      ┌────┴────┐         ┌───────┴───────┐            ║
║     │ Widget │     │ Comment │      │ Version │         │ FileReference │            ║
║     └────────┘     └─────────┘      └─────────┘         └───────────────┘            ║
║                                                                                      ║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

An issue the customer sees is therefore the tuple `(Object, Class, Workflow, Priority, Form, [Values], [Comments], [Versions], [FileReferences], [Links])`. The portal renders this tuple as a single coherent vertical timeline with an attached side panel for participants and metadata.

## Software Architecture

The application's architecture follows the same modular, decoupled design used elsewhere in **KleeneStar**. At its core is the `PortalManager`, which is exclusively responsible for orchestrating the portal-visible projection of objects and the portal-specific concerns (request type catalog, sharing, watching, resolution acceptance). Through the `IPortalManager` interface, it exposes a controlled API to portal pages and to integrations.

The `PortalManager` does not own the underlying data — issues are still `IObject` instances owned by the `ObjectManager`, classes by the `ClassManager`, comments by their respective managers, etc. Instead, the `PortalManager` composes these underlying managers into the portal-specific use cases:

- Resolving the **request type catalog** for the current identity by filtering `IClass` instances by their `PortalVisibility` flag and by the active workspace/tenant context.
- Resolving the **template list** for a given request type by querying associated `IForm` instances flagged `PortalTemplate`.
- Building an **issue projection** for a given `IObject`, including the collapsed lifecycle state, the requester, the participants (shared, watching), and the visibility-filtered timeline of comments and versions.
- Serving as the entry point for the portal-only mutations: `CreateIssue`, `AddComment`, `ShareIssue`, `Watch`, `Unwatch`, `AcceptResolution`, `RejectResolution`.

For reactive, loosely coupled communication, the `PortalManager` provides the events `IssueCreated`, `IssueUpdated`, `IssueShared`, `IssueWatched`, `IssueUnwatched`, `IssueResolutionProposed`, `IssueResolutionAccepted`, `IssueResolutionRejected`, and `IssueClosed`. These events are exposed via the **WebExpress** `EventManager` so that notification subsystems, audit trails, and integrations can subscribe without coupling to the portal directly.

```
╔KleeneStar.Portal═════════════════════════════════════════════════════════════════════╗
║                                                                                      ║
║                              ┌────────────────────┐                                  ║
║                              │ <<Interface>>      │                                  ║
║                              │ IComponentManager  │                                  ║
║                              ├────────────────────┤                                  ║
║                              └────────Δ───────────┘                                  ║
║                                       ¦                                              ║
║                     ┌─────────────────┴─────────────────────┐                        ║
║                     │ <<Interface>>                         │                        ║
║         ┌-----------┤ IPortalManager                        │                        ║
║         ¦           ├───────────────────────────────────────┤                        ║
║         ¦           │ IssueCreated:Event                    │                        ║
║         ¦           │ IssueUpdated:Event                    │                        ║
║         ¦           │ IssueCommented:Event                  │                        ║
║         ¦           │ IssueShared:Event                     │                        ║
║         ¦           │ IssueWatched:Event                    │                        ║
║         ¦           │ IssueResolutionProposed:Event         │                        ║
║         ¦           │ IssueResolutionAccepted:Event         │                        ║
║         ¦           │ IssueResolutionRejected:Event         │                        ║
║         ¦           │ IssueClosed:Event                     │                        ║
║         ¦           ├───────────────────────────────────────┤ 1                      ║
║         ¦           │ RequestTypes:                         ├───────┐                ║
║         ¦           │   IEnumerable<IRequestType>           │       │                ║
║         ¦           │ Issues:IEnumerable<IIssue>            │       │                ║
║         ¦           ├───────────────────────────────────────┤       │                ║
║         ¦           │ GetRequestTypes(IIdentity):           │       │                ║
║         ¦           │   IEnumerable<IRequestType>           │       │                ║
║         ¦           │ GetIssues(IIdentity, scope):          │       │                ║
║         ¦           │   IEnumerable<IIssue>                 │       │                ║
║         ¦           │ CreateIssue(IRequestType,             │       │                ║
║         ¦           │   ITemplate?, payload):IIssue         │       │                ║
║         ¦           │ ShareIssue(IIssue,                    │       │                ║
║         ¦           │   IEnumerable<IIdentity>):IIssue      │       │                ║
║         ¦           │ Watch(IIssue, IIdentity):IIssue       │       │                ║
║         ¦           │ Unwatch(IIssue, IIdentity):IIssue     │       │                ║
║         ¦           │ AcceptResolution(IIssue):IIssue       │       │                ║
║         ¦           │ RejectResolution(IIssue,              │       │                ║
║         ¦           │   String reason):IIssue               │       │                ║
║         ¦           └───────────────────────────────────────┘       │                ║
║         ¦                                                           │                ║
║         ¦              ┌────────────────────────────────┐           │                ║
║         ¦              │ <<Interface>>                  │           │                ║
║         ¦              │ <<from KleeneStar.Core>>       │           │                ║
║         ¦              │ IObject                        │           │                ║
║         ¦              ├────────────────────────────────┤           │                ║
║         ¦              │ Id:Guid                        │           │                ║
║         ¦              │ Key:String                     │           │                ║
║         ¦              │ Summary:String                 │           │                ║
║         ¦              │ Description:String             │           │                ║
║         ¦              │ Class:IClass                   │           │                ║
║         ¦              │ Workspace:IWorkspace           │           │                ║
║         ¦              │ State:IWorkflowState           │           │                ║
║         ¦              │ Values:IEnumerable<IValue>     │           │                ║
║         ¦              │ Created:DateTime               │           │                ║
║         ¦              │ Updated:DateTime               │           │                ║
║         ¦              │ PermissionsProfiles:           │           │                ║
║         ¦              │   IEnumerable<                 │           │                ║
║         ¦              │     IPermissionsProfile>       │           │                ║
║         ¦              └───────────────△────────────────┘           │                ║
║         ¦                              ¦  extends                   │                ║
║         ¦                              ¦                            │                ║
║         ¦       ┌──────────────────────┴─────────────┐ *            │                ║
║         ¦       │ <<Interface>>                      ◄──────────────┘                ║
║         ¦       │ IIssue : IObject                   │ ┌────────────────────┐        ║
║         ¦       ├────────────────────────────────────┤ │ <<Enum>>           │        ║
║         ¦       │ PortalState:PortalIssueState       │ │ PortalIssueState   │        ║
║         ¦       │ Priority:IPriority                 │ ├────────────────────┤        ║
║         ¦       │ RequestType:IRequestType           │ │ Open               │        ║
║         ¦       │ Requester:IIdentity                │ │ InProgress         │        ║
║         ¦       │ Assignee:IGroup                    │ │ WaitingOnRequester │        ║
║         ¦       │ SharedWith:IEnumerable<IIdentity>  │ │ Resolved           │        ║
║         ¦       │ Watchers:IEnumerable<IIdentity>    │ │ Closed             │        ║
║         ¦       │ Comments:IEnumerable<IComment>     │ └────────────────────┘        ║
║         ¦       │ History:IEnumerable<IVersion>      │                               ║
║         ¦       │ Attachments:                       │                               ║
║         ¦       │   IEnumerable<IFileReference>      │                               ║
║         ¦       └─────────────────Δ──────────────────┘                               ║
║         ¦                         ¦                                                  ║
║         ¦ create ┌────────────────┴───────────────────┐                              ║
║         └--------► Issue : Object                     │                              ║
║                  ├────────────────────────────────────┤                              ║
║                  │ ...inherits all members of Object  │                              ║
║                  │   plus the IIssue extension members│                              ║
║                  └────────────────────────────────────┘                              ║
║                                                                                      ║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

The `PortalManager` is registered as an `IComponentManager` and is resolved through the `CoreHub` via the standard `ComponentHub.GetComponentManager<T>()` pattern (see `CLAUDE.md`). It is wired up in the `KleeneStarApplication` constructor alongside the other managers and exposes its facade on `CoreHub.Portal`.

An issue is not a separate entity from `IObject` — it is the same entity, surfaced under a different vocabulary and enriched with portal-specific projections. The `IIssue` interface inherits from `IObject` defined in `KleeneStar.Core` and adds members that are only meaningful in the portal context: the collapsed `PortalState` (a projection of the underlying `IWorkflowState`), the resolved `Requester` and `Assignee`, the `SharedWith` and `Watchers` identity sets, and the navigation collections `Comments`, `History`, and `Attachments` that the core data model already links to `IObject` via `Comment`, `Version`, and `FileReference`. The concrete `Issue` class likewise inherits from `Object` and is constructed by the `PortalManager` from an existing `IObject` plus the portal-side enrichment data — no new persistence root is introduced.

A reverse index is maintained for issues to support full-text search across `Key`, `Summary`, `Description`, and the visible portion of comments. The index is segmented per tenant and per identity so that search results never leak across tenant boundaries or beyond what the calling identity is authorized to see.

Every relevant action is logged by the integrated audit system. Audit entries record the timestamp, the calling identity, the affected issue key, the action type, and — for sharing/watching/resolution-acceptance actions — the affected secondary identity. This is the same audit pipeline used by the operator WebApp; the portal does not maintain a separate trail.

## UI Concepts and Pages

The following UI mockups translate the abstract data models and lifecycle rules of the portal into a concrete and tangible end-user experience. The goal is a deliberately calm, low-density interface that requires no training: the entire portal can be operated by an end user who is not a **KleeneStar** specialist. All UI patterns are consistent with the portal's design system (`portal.css`) and intentionally diverge from the operator WebApp shell to make the audience switch obvious to anyone using both surfaces.

These mockups serve as a blueprint for the final design and specify navigation, the arrangement of controls, and the display of system states. They illustrate how end users are guided through their primary tasks: discovering a request type, submitting a request, tracking its progress, collaborating with their colleagues on it, and confirming its resolution.

### Portal Login (Page)

The portal entry point is a focused login screen. It supports password-based authentication for accounts owned by the portal and federated sign-in via the tenant's configured SSO providers (typically Microsoft and Google). A "stay signed in" option and a password reset link complete the standard set. The page is intentionally free of operator-side branding or navigation — a customer arriving at the portal must perceive it as their service portal, not as a back-office tool.

```
╔PortalLoginPage═══════════════════════════════════════════════════════════════════════╗
║                                                                                      ║
║                                                                                      ║
║                          ┌────────────────────────────────┐                          ║
║                          │ * KleeneStar                   │                          ║
║                          ├────────────────────────────────┤                          ║
║                          │                                │                          ║
║                          │ Welcome back                   │                          ║
║                          │ Sign in to the service portal  │                          ║
║                          │                                │                          ║
║                          │ Email                          │                          ║
║                          │ [ anna.becker@acme.com       ] │                          ║
║                          │                                │                          ║
║                          │ Password                       │                          ║
║                          │ [ ••••••••                   ] │                          ║
║                          │                                │                          ║
║                          │ [✓] Stay signed in   Forgot?   │                          ║
║                          │                                │                          ║
║                          │ [        Sign in             ] │                          ║
║                          │                                │                          ║
║                          │ ─────────── or ──────────────  │                          ║
║                          │                                │                          ║
║                          │ [ Continue with Microsoft    ] │                          ║
║                          │ [ Continue with Google       ] │                          ║
║                          │                                │                          ║
║                          └────────────────────────────────┘                          ║
║                                                                                      ║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Portal Home (Page)

The home page is the primary landing experience after login. It answers the question "What does the user want to do next?" with two stacked answers: pick a request type to start a new issue, or open one of the user's recent issues. The header greets the user by first name to confirm the active identity. A search bar above the request-type grid spans both templates and existing issues, providing a single jump-off point for navigation.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | My Issues | Organization      [Σ][?][AB] │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
║┌Page──Home──────────────────────────────────────────────────────────────────────────┐║
║│                                                                                    │║
║│  How can we help, Anna?                                                            │║
║│  Pick a template or browse all request types.                                      │║
║│                                                                                    │║
║│  [ Search templates or issues…                                                  ] │║
║│                                                                                    │║
║│  REQUEST TYPES                                                                     │║
║│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐                    │║
║│  │ ⚡ Report incid. │ │ + Request access │ │ ⚙ Service req.   │                    │║
║│  │ Service is down… │ │ Licenses, accs.  │ │ Configuration…   │                    │║
║│  │ □ 3 templates    │ │ □ 3 templates    │ │ □ 2 templates    │                    │║
║│  └──────────────────┘ └──────────────────┘ └──────────────────┘                    │║
║│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐                    │║
║│  │ ⓘ Question/advc. │ │ ▤ Document/ctr.  │ │ ✸ Feedback/idea  │                    │║
║│  │ General questions│ │ Contracts, orders│ │ Improvements     │                    │║
║│  │ □ 2 templates    │ │ □ 2 templates    │ │ □ 1 template     │                    │║
║│  └──────────────────┘ └──────────────────┘ └──────────────────┘                    │║
║│                                                                                    │║
║│  RECENTLY BY YOU                                                                   │║
║│  ┌──────────────────────────────────────────────────────────────────────────────┐  │║
║│  │ Key        | Title                          | Status   | Prio | Updated     │  │║
║│  │------------|--------------------------------|----------|------|-------------│  │║
║│  │ INC-2041   | Outlook not receiving mail     | InProg.  |  P2  | 2h ago      │  │║
║│  │ REQ-1284   | Office 365 license new hire    | Waiting  |  P3  | 1d ago      │  │║
║│  │ INC-2025   | Printer prints stripes         | Open     |  P3  | 5h ago      │  │║
║│  └──────────────────────────────────────────────────────────────────────────────┘  │║
║│                                                                                    │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Template Picker (Modal)

Selecting a request-type tile opens a modal that lists the templates available for that type and offers a "blank request" escape hatch. Templates are pre-bound forms that reduce the number of decisions the user has to make; a template carries a title, a short description, and (after selection) prefilled values for the form behind it.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | My Issues | Organization      [Σ][?][AB] │║
║└─────╔TemplatePickerModal═════════════════════════════════════════════════════╗─────┘║
║┌Page─║ ⚡ Report incident                                                 [X] ║─────┐║
║│     ║ Pick a template to start quickly.                                      ║     │║
║│     ╠═══════════════════════════════════════════════════════════════════════╣     │║
║│     ║ ┌──────────────────────────────────────────────────────────────────┐  ║     │║
║│     ║ │ □ Email is not working                                       ›   │  ║     │║
║│     ║ │   Sending or receiving is blocked.                               │  ║     │║
║│     ║ ├──────────────────────────────────────────────────────────────────┤  ║     │║
║│     ║ │ □ VPN drops the connection                                   ›   │  ║     │║
║│     ║ │   Connection drops repeatedly.                                   │  ║     │║
║│     ║ ├──────────────────────────────────────────────────────────────────┤  ║     │║
║│     ║ │ □ Application crashes                                        ›   │  ║     │║
║│     ║ │   Software hangs or freezes.                                     │  ║     │║
║│     ║ ├╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴╴┤  ║     │║
║│     ║ │ + Blank request                                                  │  ║     │║
║│     ║ │   Start without a template.                                      │  ║     │║
║│     ║ └──────────────────────────────────────────────────────────────────┘  ║     │║
║│     ╚═══════════════════════════════════════════════════════════════════════╝     │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Create Issue (Modal)

After picking a template (or "blank request"), the user is presented with the create-issue modal. The modal carries the request-type icon and description as its header, ensuring the user never loses context. The form itself is intentionally minimal: short title, optional details, and an urgency selector that translates immediately into an expected response time displayed below the form. Submitting the form persists the issue via `PortalManager.CreateIssue` and returns the user to a confirmation state showing the freshly-issued issue key.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | My Issues | Organization      [Σ][?][AB] │║
║└─────╔CreateIssueModal═══════════════════════════════════════════════════════╗─────┘║
║┌Page─║ ⚡ Report incident                                                 [X] ║─────┐║
║│     ║ A service is unreachable or faulty.                                    ║     │║
║│     ╠═══════════════════════════════════════════════════════════════════════╣     │║
║│     ║ Short description *                                                   ║     │║
║│     ║ [ e.g. Outlook is not receiving external mail                       ] ║     │║
║│     ║                                                                       ║     │║
║│     ║ Details                                                               ║     │║
║│     ║ [ What happened? Steps to reproduce?                                ] ║     │║
║│     ║ [                                                                   ] ║     │║
║│     ║                                                                       ║     │║
║│     ║ Urgency                                                               ║     │║
║│     ║ [ P3 — Normal                                                       ▼]║     │║
║│     ║                                                                       ║     │║
║│     ║ ⓘ Expected response: 1 business day                                   ║     │║
║│     ╠═══════════════════════════════════════════════════════════════════════╣     │║
║│     ║                                              [Cancel]  [Submit]      ║     │║
║│     ╚═══════════════════════════════════════════════════════════════════════╝     │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Issue List (Page)

The "My Issues" and "Organization" pages share a single list component differing only in scope. *My Issues* shows issues the user created or was added to. *Organization* shows all issues within the user's tenant that the active permissions profile permits them to see. A textual search filters by issue key and title; status chips above the list act both as filters and as a counter strip — each chip carries the number of issues in that state for the active scope. Clicking an issue row opens the issue drawer (see below) without navigating away, preserving the list as context.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | MY ISSUES | Organization      [Σ][?][AB] │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
║┌Page──IssueList────────────────────────────────────────────────────────────────────┐║
║│                                                                                    │║
║│  My Issues                                                  [+ New request]       │║
║│  Issues you created or are participating in.                                      │║
║│                                                                                    │║
║│  [ Search by key or title…                                                       ] │║
║│                                                                                    │║
║│  [All 3] [Open 1] [InProg. 1] [Waiting 1] [Resolved 0] [Closed 0]                  │║
║│                                                                                    │║
║│  ┌──────────────────────────────────────────────────────────────────────────────┐  │║
║│  │ Key       | Title                  | Status   | Prio | Updated | Shared     │  │║
║│  │-----------|------------------------|----------|------|---------|------------│  │║
║│  │ INC-2041  | Outlook not receiv…    | InProg.  | P2   | 2h ago  | [MS][LP]   │  │║
║│  │ REQ-1284  | Office 365 license …   | Waiting  | P3   | 1d ago  |            │  │║
║│  │ INC-2025  | Printer prints stripes | Open     | P3   | 5h ago  | [TR]       │  │║
║│  └──────────────────────────────────────────────────────────────────────────────┘  │║
║│                                                                                    │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Issue Detail (Drawer)

Opening an issue slides a right-hand drawer over the current list, preserving navigation state. The drawer's header carries the issue key, the current status pill, the priority pill, and the inline actions *Watch / Watching*, *Share*, and close. The body splits into a vertical conversation stream (description + comments + system entries) on the left and a participant/metadata side panel on the right.

When the issue is in the `Resolved` state, a banner at the top of the conversation invites the requester to **accept the resolution** (which closes the issue) or **reject** it (which sends it back to *In Progress* with a mandatory reason). System entries (status changes, share events, watch events, resolution events) are styled distinctly so they read as machine narration, not as human messages.

The composer below the timeline supports two visibilities: *Public* — the default — produces a comment visible to the requester, the service team, and everyone the issue is shared with. *Service team only* is offered as a courtesy when the underlying class permits it and limits a comment to the assigned service group plus the requester. `⌘+Enter` submits.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | My Issues | Organization      [Σ][?][AB]  │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
║┌Page──IssueList─────────────────╔IssueDrawer═════════════════════════════════════════║
║│                                ║ INC-2041 [ InProg. ] [ ● P2 ]    [Watch][Share][X] ║
║│  My Issues                     ║ Outlook not receiving external mail                ║
║│  …                             ║ □ Incident · by Anna Becker · created 2026-04-30   ║
║│                                ║ · Service Desk · Tier 2                            ║
║│  ┌─────────────────────────┐   ╠═════════════════════════════════════════════════════║
║│  │ INC-2041 …              │   ║┌Stream─────────────────────┐┌Side─────────────────┐ ║
║│  │ REQ-1284 …              │   ║│ DESCRIPTION               ││ DETAILS             │ ║
║│  │ INC-2025 …              │   ║│ Since this morning, no…   ││ Type    Incident    │ ║
║│  └─────────────────────────┘   ║│                           ││ Owner   Tier 2      │ ║
║│                                ║│ HISTORY · 4 entries       ││ Created 2026-04-30  │ ║
║│                                ║│ [AB] Anna Becker · yest.  ││ Updated 2h ago      │ ║
║│                                ║│      Restarted Outlook,…  │└─────────────────────┘ ║
║│                                ║│ [MS] Markus · Service Desk│┌Requester────────────┐ ║
║│                                ║│      Hi Anna, we're chec… ││ [AB] Anna Becker    │ ║
║│                                ║│ [AB] Anna · yesterday     ││      anna@acme.com  │ ║
║│                                ║│      Just did that.       │└─────────────────────┘ ║
║│                                ║│ [KS] System · today 08:30 │┌Shared with (2)──────┐ ║
║│                                ║│      Status changed:…     ││ [MS] Markus         │ ║
║│                                ║│                           ││ [LP] Lara           │ ║
║│                                ║│┌Composer──────────────────┤│ + Add person        │ ║
║│                                ║││ ✎ Add reply             │└─────────────────────┘ ║
║│                                ║││ [Public][Team only]      │┌Watchers (2)─────────┐ ║
║│                                ║││ [ Write a comment…     ] ││ [AB] Anna           │ ║
║│                                ║││                          ││ [MS] Markus         │ ║
║│                                ║││ [+][☺]    ⌘+Enter [Send] ││ ◉ Stop watching     │ ║
║│                                ║│└──────────────────────────┘└─────────────────────┘ ║
║│                                ╚════════════════════════════════════════════════════║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Share Issue (Modal)

The share modal is reachable from the drawer header. It allows the requester (or any identity that already has share rights on the issue) to add additional identities from the same tenant as readers/commenters. A type-ahead input matches against name and email of the tenant directory. Already-shared identities are listed as a read-only block below the input. A copyable shareable link is offered as a convenience for users who want to paste the issue location into chat — opening the link still requires the recipient to authenticate and to be authorized.

```
╔PortalShell═══════════════════════════════════════════════════════════════════════════╗
║┌Topbar──────────────────────────────────────────────────────────────────────────────┐║
║│ * KleeneStar · Service Portal     Home | My Issues | Organization      [Σ][?][AB] │║
║└─────╔ShareIssueModal════════════════════════════════════════════════════════╗─────┘║
║┌Page─║ Share issue                                                       [X] ║─────┐║
║│     ║ INC-2041 · Outlook not receiving external mail                         ║     │║
║│     ╠═══════════════════════════════════════════════════════════════════════╣     │║
║│     ║ Invite people                                                         ║     │║
║│     ║ [ [Markus ×] [Lara ×]  Name or email…                               ] ║     │║
║│     ║                                                                       ║     │║
║│     ║   ┌────────────────────────────────────────────────────────────────┐ ║     │║
║│     ║   │ [SK] Sina Köhler            sina.koehler@acme.com              │ ║     │║
║│     ║   │ [TR] Tomás Rivera           tomas.rivera@acme.com              │ ║     │║
║│     ║   └────────────────────────────────────────────────────────────────┘ ║     │║
║│     ║                                                                       ║     │║
║│     ║ Already shared (1)                                                    ║     │║
║│     ║   [MS] Markus Schmidt    markus.schmidt@acme.com         Reader      ║     │║
║│     ║                                                                       ║     │║
║│     ║ ⛓ https://portal.kleenestar.io/i/INC-2041            [Copy]          ║     │║
║│     ╠═══════════════════════════════════════════════════════════════════════╣     │║
║│     ║                                          [Cancel]  [Share (2)]       ║     │║
║│     ╚═══════════════════════════════════════════════════════════════════════╝     │║
║└────────────────────────────────────────────────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

## Sitemap

The sitemap defines the hierarchical structure and navigation paths of the user interface for the customer portal. It ensures a clear organization of the pages, serves as the basis for routing within the portal application, and is structured as follows:

|Path                            |Page                  |Description
|--------------------------------|----------------------|--------------------------------------------------------------
|`/login`                        |Portal Login          |Authentication entry point with password and SSO flows.
|`/`                             |Portal Home           |Greeting, request-type catalog, recent issues of the user.
|`/types/{requestTypeKey}`       |Template Picker       |Modal listing the templates of a request type, plus blank-request escape.
|`/types/{requestTypeKey}/new`   |Create Issue         |Modal form for submitting a new issue against the chosen request type/template.
|`/mine`                         |My Issues            |Issues the user created or was added to.
|`/org`                          |Organization Issues  |All issues in the user's tenant the active profile permits.
|`/i/{issueKey}`                |Issue Detail         |Drawer with description, history, composer, and side panel.
|`/t/{issueKey}/share`          |Share Issue          |Modal for inviting additional identities of the same tenant.
|`/t/{issueKey}/accept`         |Accept Resolution     |Idempotent route that accepts a proposed resolution and closes the issue.
|`/t/{issueKey}/reject`         |Reject Resolution     |Modal for rejecting a proposed resolution with a mandatory reason.

## API Interfaces (REST Endpoints)

For programmatic interaction, third-party integration, and automation, **KleeneStar** exposes the portal use cases as a standardized REST API alongside the existing core APIs. The interface adheres to REST principles and uses JSON as the data exchange format. Authentication and authorization are handled by **KleeneStar**, with the same identity, group, and profile semantics that drive the operator WebApp. Standard HTTP status codes indicate the outcome of each request.

The portal is served via the following endpoints:

|Endpoint                                          |HTTP Method |Description
|--------------------------------------------------|------------|------------------------------------------------------------
|`/api/1/portal/request-types`                     |GET         |Lists the request types visible to the calling identity, including their templates.
|`/api/1/portal/issues`                           |GET         |Lists issues visible to the calling identity. Supports `scope=mine\|org`, status filters, and full-text search.
|`/api/1/portal/issues`                           |POST        |Creates a new issue. Body must include `requestTypeKey`, optional `templateKey`, `title`, and the form payload.
|`/api/1/portal/issues/{issueKey}`               |GET         |Retrieves the issue projection — metadata, participants, description, comments, history, attachments.
|`/api/1/portal/issues/{issueKey}/comments`      |POST        |Adds a comment. Body carries `text` and `visibility` (`public` or `internal-team`).
|`/api/1/portal/issues/{issueKey}/share`         |POST        |Adds one or more identities of the same tenant to the issue's shared-with list.
|`/api/1/portal/issues/{issueKey}/share/{id}`    |DELETE      |Revokes a previously granted share.
|`/api/1/portal/issues/{issueKey}/watch`         |POST        |Subscribes the calling identity to the issue's notifications.
|`/api/1/portal/issues/{issueKey}/watch`         |DELETE      |Unsubscribes the calling identity.
|`/api/1/portal/issues/{issueKey}/accept`        |POST        |Accepts a proposed resolution. Idempotent — repeated calls have no further effect on a closed issue.
|`/api/1/portal/issues/{issueKey}/reject`        |POST        |Rejects a proposed resolution. Body must include a `reason`. Returns the issue to *In Progress*.
|`/api/1/portal/issues/{issueKey}/attachments`   |POST        |Uploads a `FileReference` against the issue. Multipart/form-data.

Standard error responses include `400 Bad Request` for validation errors (e.g., missing required form fields), `401 Unauthorized` for missing authentication, `403 Forbidden` for insufficient permissions or for cross-tenant share attempts, `404 Not Found` if the issue or request type does not exist or is not visible to the caller, and `409 Conflict` when an action collides with the current issue state (e.g., accepting a resolution that has not been proposed). A successful creation (POST) is acknowledged with `201 Created`; a successful state-changing action without a body returns `204 No Content`.

## Portal Events

The portal uses an event-driven architecture model to communicate state changes transparently and reactively. Events are published via the **WebExpress** `EventManager`, which acts as the central event backbone. Notification subsystems, audit pipelines, and external integrations subscribe to relevant changes without coupling to the `PortalManager`.

The following events are published by the `PortalManager`:

|Event Name                    |Description
|------------------------------|--------------------------------------------------------------------------------
|`IssueCreated`               |A new issue has been submitted from the portal.
|`IssueUpdated`               |The portal-visible projection of an issue changed — state, priority, assignee, or core metadata.
|`IssueCommented`             |A comment was added (carries the visibility flag of the comment).
|`IssueShared`                |One or more identities were added to an issue's shared-with list.
|`IssueUnshared`              |An identity's share access was revoked.
|`IssueWatched`               |An identity subscribed to an issue's notifications.
|`IssueUnwatched`             |An identity unsubscribed from an issue's notifications.
|`IssueResolutionProposed`    |The service team proposed a resolution; the requester is asked to confirm.
|`IssueResolutionAccepted`    |The requester accepted a proposed resolution; the issue is closing.
|`IssueResolutionRejected`    |The requester rejected a proposed resolution; the issue returns to *In Progress*.
|`IssueClosed`                |The issue has been closed — by acceptance, by timeout, or administratively.

The event payload includes:
- The unique issue key
- The acting identity (or `system` for automated transitions)
- Timestamp of the action
- A type-specific delta (e.g., the previous and new state for `IssueUpdated`, the affected identities for `IssueShared`)

## Permissions Model

The portal applies the **KleeneStar** permissions model context-specifically. Group memberships and policies are global; their effect on a portal interaction is mediated by the workspace profile attached to the request type's underlying class and by the issue-local sharing record.

A class becomes portal-visible when its declaration carries the `PortalVisible` flag and at least one of its associated forms is marked `PortalTemplate` (or the class itself opts into blank submissions). A user sees a request type when:

1. Their identity belongs to a group whose profile in the request type's workspace grants `portal_request_type_view`, **and**
2. The active tenant of the user matches the request type's tenant assignment.

A user sees an issue when **any** of the following holds:

1. They are the `Requester`, **or**
2. They are listed in `SharedWith`, **or**
3. Their group profile grants `portal_org_issues_view` for the workspace, **and** the issue belongs to their tenant.

The granular permissions specific to the portal are:

|Permission                          |Description
|------------------------------------|---------------------------------------------------------------------------------
|`portal_request_type_view`          |Allows seeing a request type and its templates in the catalog.
|`portal_issue_create`              |Allows submitting a new issue against a visible request type.
|`portal_issue_view_own`            |Grants access to issues where the user is requester, watcher, or shared-with.
|`portal_org_issues_view`           |Grants access to all issues in the user's tenant within a given workspace.
|`portal_issue_comment`             |Allows posting a comment with `visibility=public`.
|`portal_issue_comment_internal`    |Allows posting a comment with `visibility=internal-team`.
|`portal_issue_share`               |Allows adding identities of the same tenant to an issue's shared-with list.
|`portal_issue_watch`               |Allows subscribing/unsubscribing self to issue notifications.
|`portal_issue_resolution_accept`   |Allows accepting or rejecting a proposed resolution. Implicitly held by the requester.

These permissions are bundled into logical policies that represent typical end-user roles:

|Policy                       |Description                                                          |Included Permissions
|-----------------------------|----------------------------------------------------------------------|-------------------------
|`portal_requester_policy`    |The default end-user role for any tenant member.                      |`portal_request_type_view`, `portal_issue_create`, `portal_issue_view_own`, `portal_issue_comment`, `portal_issue_share`, `portal_issue_watch`, `portal_issue_resolution_accept`
|`portal_org_observer_policy` |Read-only access to all issues in the user's tenant.                 |`portal_request_type_view`, `portal_issue_view_own`, `portal_org_issues_view`, `portal_issue_watch`
|`portal_org_lead_policy`     |Tenant-side leadership: read-all and the ability to comment publicly. |`portal_request_type_view`, `portal_issue_view_own`, `portal_org_issues_view`, `portal_issue_comment`, `portal_issue_share`, `portal_issue_watch`

The internal-team comment permission (`portal_issue_comment_internal`) is intentionally not part of any portal policy — it is granted to service-team groups via operator-side policies and surfaces in the portal composer only when the calling identity holds it.

## Conclusion

The document "KleeneStar Customer Portal" provides the conceptual foundation for the portal reference implementation. It defines the portal as a constrained, customer-facing projection of the existing **KleeneStar** core data model, the lifecycle of an issue from submission to closure, the request-type catalog with templates, the conversation and resolution-confirmation flows, and the role of `PortalManager` in orchestrating these use cases on top of the existing managers. As a high-level blueprint, it intentionally omits technical detail in several areas. Topics such as notification delivery (e-mail, push, in-app), attachment storage policies, rate-limiting against abusive submissions, and the SLA timer behind the *Expected response* hint are left open and must be defined during implementation. Similarly, internationalization beyond the prototype, accessibility audits, and mobile-specific layout refinements are not covered. Long-running operations such as bulk-import of issues and the audit-trail retention policy that backs portal events are likewise out of scope. The document thus offers a solid conceptual base while leaving critical implementation aspects open. The reference implementation is expected to close these gaps with concrete solutions and validate the practical feasibility of the proposed model.
