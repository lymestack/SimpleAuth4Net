/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { MfaMethod } from "./mfa-method";

export class LoginModel {
    username: string;
    password: string;
    deviceId: string;
    mfaMethod: MfaMethod = 1;
}
