# Play Data safety ↔ privacy policy

The Play Console Data safety form and [the privacy policy](public/privacy/index.html) must say the same thing (017 FR-019). When either one changes, walk this table and update the other. The policy itself is written from [016's data-retention.md](../specs/016-account-deletion/data-retention.md).

## Data types

**Play's terms used in the tables:**

- **Collected** means the data leaves the device.
- **Shared** means it goes to a third party for that party's own purposes. Service providers acting for LoopMeet (Supabase, DigitalOcean, OneSignal, Google Places, FCM, Cloudflare) are *not* sharing.
- **Ephemeral** means it's processed in memory only and never stored.

| Play category → type | Policy section | Collected | Shared | Ephemeral | Required or optional | Purposes |
| --- | --- | --- | --- | --- | --- | --- |
| Personal info → Name | 2, Account | Yes | No | No | Required (display name) | App functionality, Account management |
| Personal info → Email address | 2, Account; 2, What you create (invitee's email) | Yes | No | No | Required | App functionality, Account management |
| Personal info → User IDs | 2, Account (Google or Apple link); 2, Sign in with Apple | Yes | No | No | Required | App functionality, Account management |
| Personal info → Phone number | 2, Account | Yes | No | No | Optional | App functionality |
| Photos and videos → Photos | 2, Profile photo | Yes | No | No | Optional | App functionality |
| Location → Approximate location | 2, Place search; 4, Location | Yes | No | **Yes** | Optional | App functionality |
| Location → Precise location | 2, Place search; 4, Location | Yes | No | **Yes** | Optional | App functionality |
| App activity → In-app search history | 2, Place search | Yes | No | **Yes** | Optional | App functionality |
| App activity → Other user-generated content | 2, What you create (groups, invitations, meetups) | Yes | No | No | Optional | App functionality |
| App activity → Other actions | 5, Push notifications (delivery and open records) | Yes | No | No | Optional | App functionality |
| App info and performance → Diagnostics | 2, Operational logs | Yes | No | No | Required | App functionality (operations and fixing problems) |
| Device or other IDs | 2, Push notifications; 5 | Yes | No | No | Required | App functionality |

**Not collected:**

- financial info
- health and fitness
- messages
- audio
- files and documents
- calendar
- contacts
- web browsing
- installed apps
- crash logs

The app contains no analytics or advertising SDKs (policy section 1).

## Location

The Android manifest declares `ACCESS_FINE_LOCATION` and `ACCESS_COARSE_LOCATION`, and place search needs precise location, so both location types are declared.

- **Ephemeral:** the location is sent with the search to Google Places through the Api, and never stored. Section 4 of the policy says so.
- **Optional:** place search works without location.
- **If the app's location use changes,** for example storing it or using it in the background, update this table, policy section 4, and the Play form together.

## Security practices

| Question | Answer | Policy section |
| --- | --- | --- |
| Is all user data encrypted in transit? | Yes (HTTPS to Supabase, the Api and OneSignal) | 3 |
| Can users request that their data be deleted? | Yes: in the app (Profile → Delete Account) and at the web deletion URL, the site's `/account/` | 6, 7 |
| Account deletion URL | `https://<site domain>/account/` (or `/delete-account`, which redirects there) | 6 |
| Privacy policy URL | `https://<site domain>/privacy/` | |

## Keep in step

- **Policy changes:** a new data type, processor or retention period in the app needs three updates:
  - the policy's tables
  - this file
  - the Play form

  `web/tests/privacy-policy.test.js` lists the categories and processors it expects, so update it too.
- **Retention values:** these come from `data-retention.md`. The policy test reads that file, and fails if the two drift apart.
