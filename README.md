![KleeneStar](https://raw.githubusercontent.com/kleenestar-project/.github/main/docs/assets/img/banner.png)

# KleeneStar.Portal - The Customer-Facing Surface

**KleeneStar.Portal** is the customer-facing surface of the **KleeneStar** platform. It provides end users and organizations with a central, low-friction interface to submit, track, share, and resolve their issues without ever needing to learn the operator-side WebApp.

Built on the **WebExpress-Framework**, it runs as a self-contained application alongside the operator WebApp inside the same host, sharing the data layer, manager hub, and identity model while shipping its own pages, navigation shell, and theme.

## What It Powers

**KleeneStar.Portal** is a deliberately reduced projection of **KleeneStar** for non-specialist audiences. Internal classes, fields, workflows, and dashboards are hidden; instead, the portal presents pre-curated **request types**, pre-defined **templates**, and a personalized list of **issues** that the user has created, has access to, or has been invited to.

Every interaction the portal offers maps to operations defined in **KleeneStar.Core**, but is constrained by tenant scope, identity context, and the active permissions profile. The `PortalManager` orchestrates these constraints and exposes them to the portal pages and to integrations as a controlled REST API.

## For Developers

This repository is the starting point for building, customizing, or extending the customer portal experience. It includes the portal application, the `PortalManager` and its `IIssue : IObject` extension over the core object model, the portal pages and components, the portal-specific REST endpoints, and the conceptual documentation that drives the implementation.

The authoritative concept document is `docs/kleenestar.portal.md`. Read it before extending request types, the issue lifecycle projection, sharing/watching, or the portal permission set.

## Legal & Licensing

**KleeneStar.Portal** is released under the `MIT License`, a permissive open-source license that allows reuse, modification, and distribution with minimal restrictions. You're free to use **KleeneStar** in personal, academic, or commercial projects, just include the original copyright notice.

The system is designed to be GDPR-compliant:
- No tracking
- No monetization
- No hidden dependencies
- Full transparency and infrastructure control

**KleeneStar** respects your data and your autonomy. It's built for clarity, not surveillance.

## Contributing

We welcome contributions in many areas:
- Portal feature development (C#)
- UI design and frontend components (JS/TS)
- Documentation and onboarding flows
- Accessibility, internationalization, and mobile refinements

Feel free to fork the repository, open issues, or submit pull requests. For larger contributions, please reach out via kleenestar.project@gmail.com.

---

Become part of the **KleeneStar** community and contribute to a modular, open-source future.
