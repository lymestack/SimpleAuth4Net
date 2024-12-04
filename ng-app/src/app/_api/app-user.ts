/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { AppUserCredential } from "./app-user-credential";
import { AppUserRole } from "./app-user-role";
import { AppRefreshToken } from "./app-refresh-token";

export class AppUser {
    id: number;
    username: string;
    emailAddress: string;
    firstName: string;
    lastName: string;
    dateEntered: Date;
    lastSeen: Date;
    appUserCredential: AppUserCredential = {"appUserId":0,"appUser":null,"passwordSalt":null,"passwordHash":null,"dateCreated":"0001-01-01T00:00:00","passwordResetToken":null,"passwordResetExpires":"0001-01-01T00:00:00","passwordResetUsed":false};
    appUserRoles: AppUserRole[] = [];
    appRefreshTokens: AppRefreshToken[] = [];
    roles: string[] = [];
    sessionId: string;
}
