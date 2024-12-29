/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { AppUser } from "./app-user";

export class AppUserPasswordHistory {
    id: number;
    appUserId: number;
    hashedPassword: number[];
    salt: number[];
    dateCreated: Date;
    appUser: AppUser;
}
