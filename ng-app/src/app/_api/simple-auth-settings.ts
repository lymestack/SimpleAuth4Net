/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Environment } from "./environment";
import { ClientSsoProviderSettings } from "./client-sso-provider-settings";

export class SimpleAuthSettings {
    environment: Environment;
    googleClientId: string;
    sessionId: string;
    enableLocalAccounts: boolean;
    allowRegistration: boolean;
    requireUserVerification: boolean;
    enableMfaViaEmail: boolean;
    enableMfaViaSms: boolean;
    enableMfaViaOtp: boolean;
    resendCodeDelaySeconds: number;
    ssoProviders: ClientSsoProviderSettings[] = [];
}
