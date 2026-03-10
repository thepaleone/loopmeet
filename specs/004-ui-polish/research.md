# Research: UI Polish — Icons & Button Placement

**Branch**: `004-ui-polish` | **Date**: 2026-03-09

## 1. Icon Delivery in MAUI

### Decision
New icon assets (logout, save, invite/send) will be delivered as SVG + PNG fallback pairs, matching the tab navigation convention: one `.svg` file and one `_fallback.png` file placed in `src/LoopMeet.App/Resources/Images/`.

### Rationale
- All four tab bar icons follow this pattern (`tab_home.svg` + `tab_home_fallback.png`, etc.).
- MAUI resolves SVG at build time and falls back to PNG at runtime on platforms that do not support SVG natively.
- This guarantees cross-platform availability (iOS, Android, macOS, Windows) without any third-party icon library.
- The existing icon set uses this pattern exclusively; adding new files here keeps asset management consistent and centralised.

### Assets Required

| Purpose | SVG file | PNG fallback |
|---------|----------|--------------|
| Logout button | `ic_logout.svg` | `ic_logout_fallback.png` |
| Save button | `ic_save.svg` | `ic_save_fallback.png` |
| Invite / Send button | `ic_invite.svg` | `ic_invite_fallback.png` |

### Alternatives Considered
- **Unicode characters only** (like ✓ and 🗑 on the swipe actions): rejected for logout/save/invite because these actions have no universally recognised Unicode symbols that are visually distinct and professionally appropriate.
- **Third-party icon library (FontAwesome, Material Icons via NuGet)**: rejected — adds a dependency, increases package size, and is not used anywhere else in the project.

---

## 2. Adding Icons to MAUI Buttons

### Decision
Use the **`Button.ImageSource` + `Button.ContentLayout`** native MAUI approach for all new iconified buttons (Logout on Profile, Save on Profile, Invite on Group Detail, Send on Invite Member). This keeps them as standard `<Button>` elements — no layout change required.

### Rationale
- Native MAUI `Button` supports `ImageSource` (any MAUI image source including bundled files) and `ContentLayout` (controls whether image appears left/right/top/bottom of text and the spacing).
- This is the simplest approach — no extra layout wrappers, no `TapGestureRecognizer` hacks.
- The existing `BubbleCardBorderStyle` + `TapGestureRecognizer` pattern (used in `InvitationDetailPage`) is appropriate for custom-styled action tiles, not for form/action buttons like Save and Logout which already use `<Button>`.
- Consistency: changing Save from `<Button>` to a `<Border>` block just to add an icon would be unnecessary complexity (Constitution Principle IV: Simplicity Over Cleverness).

### ContentLayout Configuration
```
ContentLayout="Left,8"    ← icon 8px to left of label text
```

### Alternatives Considered
- **Border + HorizontalStackLayout + Label** (existing swipe/detail pattern): appropriate for the card-style Accept/Decline tiles in InvitationDetailPage but overly complex for simple form buttons.
- **FontImageSource with unicode glyph**: rejected — relies on a specific font being registered; not all Unicode symbols render consistently across platforms.

---

## 3. Desktop Accept & Decline Buttons — Icon Approach

### Decision
The desktop Accept button icon and the new desktop Decline button icon will use the same Unicode characters as the corresponding swipe actions:
- Accept: Unicode checkmark `✓`
- Decline: Unicode trash/bin `🗑`

These are rendered as `Label` elements within a `HorizontalStackLayout` inside each button's visual, matching the existing swipe item structure. The desktop buttons will use the same `<Button>` element pattern as the current Accept button, with icon text embedded as a prefix label inside a `HorizontalStackLayout` template, OR simply using the button's `Text` property to include the symbol inline (e.g., `"✓ Accept"`).

### Rationale
- The swipe actions already render these characters successfully on all four platforms (iOS, Android, macOS, Windows) — they are proven cross-platform.
- Using the same characters ensures visual identity between the swipe action and the desktop button: users immediately recognise the action.
- No new asset files required for Accept/Decline icons.

### Implementation Note
The simplest approach for the desktop button icon is to prepend the Unicode character inline in the button text (`Text="✓ Accept"`, `Text="🗑 Decline"`). This is simpler than wrapping in a custom layout. The styling is handled by the existing primary button style.

### Alternatives Considered
- **New SVG asset for Accept/Decline**: rejected — the Unicode characters are already established in the app's visual language for these actions; introducing different assets would create inconsistency.

---

## 4. Floating Action Button (FAB) for Create Group

### Decision
Implement the FAB using a **`Grid` overlay pattern**: the Groups page root layout changes from `VerticalStackLayout`/`ScrollView` to an `AbsoluteLayout` (or `Grid` with RowSpan) so the circular button can float above the scrollable content at an absolute bottom-right position.

### Rationale
- MAUI has no native FAB control. The standard pattern is `AbsoluteLayout` with `AbsoluteLayout.LayoutFlags` and `AbsoluteLayout.LayoutBounds` to pin a child to a corner, or a `Grid` where the FAB spans all rows with `VerticalOptions="End"` and `HorizontalOptions="End"`.
- `Grid` with row-spanning FAB is simpler than `AbsoluteLayout` for this use case since the page already uses a structured layout.
- A circular button is achieved by setting `WidthRequest`, `HeightRequest` (equal values, e.g., 56×56), and `CornerRadius` equal to half (28) to produce a perfect circle.
- Bottom padding must be added to the list `CollectionView`/`ScrollView` to prevent the FAB from permanently obscuring the last list item.

### FAB Specification
- Shape: Circle (equal width/height, corner radius = half the side length)
- Size: 56×56 dp (standard FAB sizing)
- Icon: "+" character (FontSize 28, white, bold) — no image asset required; the "+" sign is universally recognisable and renders identically on all platforms
- Colour: Primary orange (#FF9F1C), matching app primary button colour
- Position: Bottom-right, 16dp margin from screen edges
- Shadow: Consistent with BubbleCardBorderStyle shadow (Radius 14, Opacity 0.20)

### Alternatives Considered
- **AbsoluteLayout**: also valid, but requires `AbsoluteLayout.LayoutFlags` attributes on every child, which is more verbose.
- **CommunityToolkit.Maui overlay/drawer**: CT.Maui 14.0.0 is already installed but has no FAB control; would require a third-party package.
- **Keeping the text button but styling it as a circle**: identical approach to what's decided — the only question was layout container.

---

## 5. Logout Command — Move to ProfileViewModel

### Decision
Move the `LogoutAsync()` method and `LogoutCommand` from `GroupsListViewModel` to `ProfileViewModel`. The `IAuthService` dependency is already injected into `ProfileViewModel` (it uses Supabase auth elsewhere), so the move requires no new service registrations.

### Rationale
- The logout logic (`_authService.SignOutAsync()` → `Shell.Current.GoToAsync("//login")`) is self-contained and does not depend on Groups-specific state.
- `ProfileViewModel` already holds user identity information and has a natural relationship with session management.
- Removing the command from `GroupsListViewModel` and its XAML binding cleans up the Groups page without breaking anything else.

### Risk
- Verify that no other page or service currently calls `LogoutCommand` via `GroupsListViewModel` binding — a quick grep confirms the command is only bound in `GroupsListPage.xaml`.

---

## 6. Testing Approach

### Decision
UI changes of this nature (icon additions, button relocation, layout changes) are verified through:
1. **Existing ViewModel unit tests** — confirm `LogoutCommand` remains functional after moving to `ProfileViewModel` (add unit test in `LoopMeet.App.Tests`).
2. **Manual acceptance testing** on each platform — automated UI tests for MAUI across four platforms are high-cost; the acceptance scenarios from the spec serve as the manual test script.
3. **No new integration tests required** — no API contracts, data models, or server-side behaviour change.

### Framework
xUnit 2.9.3 (already configured in `LoopMeet.App.Tests`).
