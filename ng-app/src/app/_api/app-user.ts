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
    verified: boolean;
    active: boolean = true;
    locked: boolean;
    appUserCredential: AppUserCredential;
    appUserRoles: AppUserRole[] = [];
    appRefreshTokens: AppRefreshToken[] = [];
    roles: string[] = [];
    sessionId: string;
}
