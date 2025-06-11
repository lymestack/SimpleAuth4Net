/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { AppUserCredential } from "./app-user-credential";
import { AppUserRole } from "./app-user-role";
import { AppRefreshToken } from "./app-refresh-token";
import { AppUserPasswordHistory } from "./app-user-password-history";

export class AppUser {
    id: number;
    username: string;
    emailAddress: string;
    firstName: string;
    lastName: string;
    phoneNumber: string;
    phoneNumberVerified: boolean;
    dateEntered: Date;
    lastSeen: Date;
    verified: boolean;
    active: boolean = true;
    locked: boolean;
    preferredMfaMethod: number;
    appUserCredential: AppUserCredential;
    appUserRoles: AppUserRole[] = [];
    appRefreshTokens: AppRefreshToken[] = [];
    appUserPasswordHistories: AppUserPasswordHistory[] = [];
    roles: string[] = [];
    sessionId: string;
    newPassword: string;
}
