/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Environment } from "./environment";

export class AppConfig {
    environment: Environment;
    googleClientId: string;
    sessionId: string;
    enableLocalAccounts: boolean;
    enableGoogleSso: boolean;
    enableFacebookSso: boolean;
    facebookAppId: string;
    allowRegistration: boolean;
    requireUserVerification: boolean;
    enableMfaViaEmail: boolean;
    enableMfaViaSms: boolean;
    enableMfaViaOtp: boolean;
    resendCodeDelaySeconds: number;
}
