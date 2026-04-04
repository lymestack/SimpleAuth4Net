namespace SimpleAuthNet.Models.Config;

public enum SimpleAuthMode
{
    Standalone,        // Current behavior, no change
    IdentityProvider,  // LymeAuth — full auth endpoints, issues tokens
    RelyingApp         // Other apps — validates tokens, local roles only
}
