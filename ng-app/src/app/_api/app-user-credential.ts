/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { AppUser } from "./app-user";

export class AppUserCredential {
    appUserId: number;
    appUser: AppUser;
    passwordSalt: number[];
    passwordHash: number[];
    dateCreated: Date;
    verifyToken: string;
    verifyTokenExpires: Date;
    verifyTokenUsed: boolean;
}
