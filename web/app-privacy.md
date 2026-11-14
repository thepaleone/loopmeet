# App Store App Privacy ↔ privacy policy

App Store Connect's App Privacy answers and [the privacy policy](public/privacy/index.html) must say the same thing, just as Play's Data safety form must ([data-safety.md](data-safety.md)). When either one changes, walk this table and update the other. The policy itself is written from [016's data-retention.md](../specs/016-account-deletion/data-retention.md).

## Apple's terms

- **Collected** means the data is sent off the device and kept longer than it takes to serve the request. LoopMeet also declares data that goes to a third party even when it isn't kept (see Location below).
- **Linked to the user** means it's tied to the account or the device, here through the user id.
- **Tracking** means linking the data with other companies' data for advertising, or sharing it with a data broker. LoopMeet does neither.

Service providers acting for LoopMeet (policy section 3) don't change the answers.

## Data types

Every purpose is **App Functionality**: no analytics, advertising, personalization or product-personalization use (policy section 1).

| App Store category → type | Policy section | Linked to the user | Used for tracking | Purposes |
| --- | --- | --- | --- | --- |
| Contact Info → Name | 2, Account | Yes | No | App Functionality |
| Contact Info → Email Address | 2, Account; 2, What you create (invitee's email) | Yes | No | App Functionality |
| Contact Info → Phone Number | 2, Account (optional) | Yes | No | App Functionality |
| User Content → Photos or Videos | 2, Profile photo | Yes | No | App Functionality |
| User Content → Other User Content | 2, What you create (groups, invitations, meetups) | Yes | No | App Functionality |
| Identifiers → User ID | 2, Account (Google or Apple link); 2, Sign in with Apple | Yes | No | App Functionality |
| Identifiers → Device ID | 2, Push notifications; 5 | Yes | No | App Functionality |
| Location → Precise Location | 2, Place search; 4, Location | No | No | App Functionality |
| Location → Coarse Location | 2, Place search; 4, Location | No | No | App Functionality |
| Search History | 2, Place search | No | No | App Functionality |
| Usage Data → Product Interaction | 5, Push notifications (delivery and open records) | Yes | No | App Functionality |
| Diagnostics → Other Diagnostic Data | 2, Operational logs | Yes | No | App Functionality |

**Not collected:**

- Health and Fitness
- Financial Info
- Sensitive Info
- Contacts (the device address book; an invitee's email is typed in, and declared above)
- Emails or Text Messages
- Audio Data, Gameplay Content, Customer Support
- Browsing History
- Purchases
- Advertising Data
- Crash Data and Performance Data (no crash-reporting or analytics SDK; TestFlight crash reports come from Apple, not the app)

## Tracking

**No.** The app has no advertising or analytics SDKs, never shares data with data brokers, and doesn't show the App Tracking Transparency prompt. `PrivacyInfo.xcprivacy` sets `NSPrivacyTracking` to false and lists no tracking domains.

## Location

- **Declared, not linked:** the location goes with the search text to Google Places through the Api, and is never stored (policy section 4). Apple's exception for data that isn't kept could apply, but Google Places is a third party, so declaring it is the conservative answer and matches the Play form.
- **Precise and Coarse:** iOS lets the user allow approximate location instead of precise, so both are declared.
- **Optional:** place search works without location.
- **If the app's location use changes,** for example storing it or using it in the background, update this table, `data-safety.md`, policy section 4, and both store forms together.

## Keep in step

- **Policy changes:** a new data type, processor or retention period needs four updates:
  - the policy's tables
  - this file and `data-safety.md`
  - App Store Connect's App Privacy answers
  - the Play Data safety form

  `web/tests/privacy-policy.test.js` lists the categories and processors it expects, so update it too.
- **New required-reason APIs** (for example a new SDK using `UserDefaults` or file timestamps) go in `src/LoopMeet.App/Platforms/iOS/Resources/PrivacyInfo.xcprivacy`. `IosAppStoreReleaseTests` pins that list.
