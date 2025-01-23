# Background

## IMPLEMENTING AUTH SUCKS

SimpleAuth.NET is a product of accumulated frustration with the task of implementing authorization / authentication into .NET WebApi based applications. The "Auth" academic is tedious, temperamental and for some reason, never seems to get any easier to work with.

Not only does implementing auth suck, but it's often the first thing that needs to be built into any semi-serious web application. At minimum, a user needs to be able to sign into an app so that it can deliver a personalized experience. This prerequisite can impede the start of new projects by a significant amount of time depending on what provider you are dealing with and how good or bad their documentation is. This keeps you from working on the fun stuff.

The process usually goes something like this:

1. Pick from a wide-ranging list of identity options / SSO providers to implement. Options are vast - local, cloud, hybrid, commercial, open source, roll your own?
2. Search the web for implementation guides in the given tech stack.
3. Follow any number of tutorials to the very end to only have them NOT WORK, leaving me to start from the beginning. These products and SSO providers are so big that any free support support are non existent, so debugging is up to you.
4. Eventually give up and move on to the next tutorial.
5. Repeat until something works.

It shouldn't be this hard.

## Why SimpleAuth?

SimpleAuth was created to simplify the process of implementing a self-hosted auth framework for apps using .NET WebApi as a back end.

## Why Self Hosted?

Apps that can run on-premises and offer multiple SSO options are more resilient. They’re resistant to internet outages or any one provider going down. This becomes even more important for apps running in:

- Secure labs
- Disconnected environments (off-grid)
- Industries with strict regulatory requirements

An ideal solution would give administrators the flexibility to turn on or off local accounts or cloud providers as needed—without tying you to a single solution or provider.

## Why Not ASP .NET Core Identity?

Microsoft Identity works, is tried and true, but it has two big downsides:

1. It’s complex: There’s a steep learning curve, even for experienced developers. [Check out their documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) to prove the point.
1. It lacks a management UI out-of-box or any polished front-end implementation: You’re still stuck building out UI for common workflows like registration / password resets and tools to manage users, roles, and tokens yourself.

So if you're going to go through all that trouble, you might as well roll-your-own instead of introducing and understanding Microsoft's layers of abstraction? Developer note: That's actually how this project started. When going down this road, I eventually threw my hands up out of frustration and said, "Screw it. I'll just make it myself."

## Why Not Cloud Providers?

Simple: They're expensive. Most of them start off free, but expense rises quickly after reaching a certain threshold, especially in commercial scenarios.

## Disable Local Accounts for Added Security

For much of the time, local accounts are enough security for a high percentage of the secured apps out there. Non-mission critical, non-sensitive product or service providers. However, if you're nervous about storing passwords in your database, you can disable local accounts authentication and use SSO providers for authentication instead, while still storing your user's data. This allows credentials and MFA workflows to be managed by SSO providers, but still allowing you to authorize with your controllers and relate to user records in your database.
