# SimpleAuth Email Duplicate Validation Fix

This document describes the changes needed to add email duplicate validation when creating or editing users in a SimpleAuth-based application.

## Problem

When creating a new user with an email address that already exists in the system:
- The database rejects the insert with a unique constraint violation (500 error)
- The Save button becomes disabled with no visible error message
- Users have no way to know why they can't proceed

## Solution Overview

1. **Backend**: Add email duplicate check in the `AppUserController.Post()` method
2. **Frontend**: Add real-time email availability checking with user feedback
3. **Frontend**: Add error handling in `onSave()` to display API validation errors

---

## Backend Changes

### File: `WebApi/Controllers/AppUserController.cs`

Add email duplicate validation in the `Post` method, after the username validation and before saving field values:

```csharp
// Check if email is already taken by another user
if (!string.IsNullOrEmpty(value.EmailAddress))
{
    var emailExists = await db.AppUsers
        .AnyAsync(x => x.EmailAddress.ToLower() == value.EmailAddress.ToLower() && x.Id != dbItem.Id);
    if (emailExists)
    {
        return BadRequest(new { error = "EMAIL_TAKEN", message = "A user with this email address already exists." });
    }
}
```

### File: `WebApi/Controllers/AuthController.cs`

Add an endpoint to check email availability (if not already present):

```csharp
[HttpGet("EmailExists")]
public async Task<IActionResult> EmailExists([FromQuery] string email, [FromQuery] int? userId = null)
{
    if (string.IsNullOrEmpty(email)) return BadRequest("Email must be provided.");

    // Case-insensitive check
    var query = db.AppUsers.Where(x => x.EmailAddress.ToLower() == email.ToLower());

    // If userId provided (edit mode), exclude that user from the check
    if (userId.HasValue)
    {
        query = query.Where(x => x.Id != userId.Value);
    }

    var appUser = await query.SingleOrDefaultAsync();
    var exists = appUser != null;
    return Ok(new { exists });
}
```

---

## Frontend Changes

### File: `user-form.component.ts`

#### 1. Add state variables for email validation

```typescript
emailAddressValid: boolean;
checkingEmailAddress: boolean;
checkedEmailAddress: boolean;
emailAddressAvailable: boolean;
originalEmailAddress: string = '';
private lastCheckedEmail: string = '';
```

#### 2. Initialize state for existing users in `ngOnInit()`

When loading an existing user, mark the email as valid:

```typescript
if (AppUserId) {
  this.rest.getResource('AppUser', AppUserId).subscribe((data) => {
    this.model = data;
    this.originalEmailAddress = data.emailAddress || '';
    this.lastCheckedEmail = data.emailAddress || '';
    // Mark email as valid for existing users with an email
    if (data.emailAddress) {
      this.emailAddressValid = true;
      this.emailAddressAvailable = true;
      this.checkedEmailAddress = true;
    }
    // ... rest of existing code
  });
}
```

#### 3. Add error handling in `onSave()`

Change from simple `.subscribe()` to handle errors:

```typescript
onSave() {
  this.saving = true;
  this.model.userLogins = [];
  this.model.userLogins.push(this.userLogin);
  this.rest.postResource('AppUser', this.model).subscribe({
    next: (data) => {
      this.logger.success('User saved.');
      this.router.navigateByUrl('/auth-admin/users');
    },
    error: (err) => {
      this.saving = false;
      const errorCode = err?.error?.error;
      const errorMessage = err?.error?.message || 'Failed to save user. Please try again.';

      if (errorCode === 'EMAIL_TAKEN') {
        this.emailAddressAvailable = false;
        this.checkedEmailAddress = true;
      } else if (errorCode === 'USERNAME_TAKEN') {
        this.usernameAvailable = false;
        this.checkedUsername = true;
      }

      this.logger.error(errorMessage);
    }
  });
}
```

#### 4. Add `onEmailFieldChanged()` method

```typescript
onEmailFieldChanged(email: string) {
  // Validate format first
  if (!email) {
    this.emailAddressValid = false;
    this.checkedEmailAddress = false;
    this.checkingEmailAddress = false;
    return;
  }

  const isValid = this.validateEmail(email);
  if (!isValid) {
    this.emailAddressValid = false;
    this.checkedEmailAddress = false;
    this.checkingEmailAddress = false;
    return;
  }

  this.emailAddressValid = true;

  // Skip check if editing and email unchanged from original (case-insensitive)
  if (this.model.id && email.toLowerCase() === this.originalEmailAddress.toLowerCase()) {
    this.checkedEmailAddress = true;
    this.emailAddressAvailable = true;
    this.checkingEmailAddress = false;
    return;
  }

  // Skip check if we already checked this exact email
  if (email.toLowerCase() === this.lastCheckedEmail.toLowerCase()) {
    return;
  }

  // Check email availability via API
  this.checkingEmailAddress = true;
  this.checkedEmailAddress = false;

  let url = `Auth/EmailExists?email=${encodeURIComponent(email)}`;
  if (this.model.id) {
    url += `&userId=${this.model.id}`;
  }

  this.rest.getResource(url).subscribe({
    next: (data: any) => {
      this.lastCheckedEmail = email;
      this.checkedEmailAddress = true;
      this.checkingEmailAddress = false;
      this.emailAddressAvailable = !data.exists;
    },
    error: (error) => {
      this.checkingEmailAddress = false;
      this.checkedEmailAddress = false;
      this.logger.error('Error checking email availability. Please try again.');
    }
  });
}
```

#### 5. Add or update `validateEmail()` method

```typescript
private validateEmail(email: string): boolean {
  if (!email) return false;

  const emailRegex =
    /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;

  return emailRegex.test(String(email).toLowerCase());
}
```

#### 6. Update `isFormValid()` if present

```typescript
isFormValid(): boolean {
  // ... existing checks ...

  // Email validation
  if (!this.emailAddressValid) return false;
  if (this.checkedEmailAddress && !this.emailAddressAvailable) return false;

  return true;
}
```

---

### File: `user-form.component.html`

#### 1. Add blur event to email input

```html
<input
  matInput
  #emailField="ngModel"
  name="emailAddress"
  required
  type="email"
  pattern="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
  [(ngModel)]="model.emailAddress"
  (blur)="onEmailFieldChanged(model.emailAddress)"
/>
```

#### 2. Add validation feedback below the input

```html
<mat-error *ngIf="emailField.invalid && emailField.touched">
  Please enter a valid email address
</mat-error>

<!-- Availability checking feedback -->
<mat-hint *ngIf="checkingEmailAddress">
  <i class="fa fa-spin fa-spinner"></i> Checking availability...
</mat-hint>

<mat-hint *ngIf="checkedEmailAddress && !checkingEmailAddress && !emailAddressAvailable" class="error-hint">
  <span style="color: #f44336">
    <i class="fa fa-exclamation-triangle"></i> This email
    address is already in use. Please enter a different email.
  </span>
</mat-hint>
```

#### 3. Update Save button disabled condition

Add email validation to the disabled condition:

```html
<button
  type="submit"
  mat-raised-button
  (click)="onSave()"
  color="primary"
  [disabled]="
    saving ||
    (model.id === 0 && !usernameAvailable) ||
    !emailAddressValid ||
    (checkedEmailAddress && !emailAddressAvailable) ||
    !userForm.valid ||
    !selectedWholesaler ||
    !rolesSelected()
  "
>
```

---

## Testing

1. Go to the user add page
2. Fill out all required fields
3. Enter an email that already exists in the system
4. Tab/click out of the email field
5. Verify:
   - Spinner shows briefly while checking
   - Error message appears: "This email address is already in use..."
   - Save button remains disabled
   - Toast notification may also appear

6. Change to a unique email
7. Verify Save button becomes enabled and form can be submitted
