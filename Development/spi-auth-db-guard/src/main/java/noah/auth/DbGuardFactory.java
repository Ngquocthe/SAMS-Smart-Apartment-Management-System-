package noah.auth;

import org.keycloak.Config;
import org.keycloak.authentication.Authenticator;
import org.keycloak.authentication.AuthenticatorFactory;
import org.keycloak.models.AuthenticationExecutionModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.KeycloakSessionFactory;
import org.keycloak.provider.ProviderConfigProperty;

import java.util.List;

public class DbGuardFactory implements AuthenticatorFactory {
    public static final String ID = "db-guard-authenticator";

    @Override public String getId() { return ID; }
    @Override public String getDisplayType() { return "DB Guard: Check username/email + buildingId"; }
    @Override public String getReferenceCategory() { return ""; }
    @Override public String getHelpText() { return "Validate user & building in external SQL Server before password check"; }
    @Override public Authenticator create(KeycloakSession session) { return new DbGuardAuthenticator(); }
    @Override public void init(Config.Scope scope) {}
    @Override public void postInit(KeycloakSessionFactory factory) {}
    @Override public void close() {}
    @Override public List<ProviderConfigProperty> getConfigProperties() { return List.of(); }
    @Override public boolean isConfigurable() { return false; }
    @Override public AuthenticationExecutionModel.Requirement[] getRequirementChoices() {
        return new AuthenticationExecutionModel.Requirement[] {
                AuthenticationExecutionModel.Requirement.REQUIRED,
                AuthenticationExecutionModel.Requirement.DISABLED
        };
    }
    @Override public boolean isUserSetupAllowed() { return false; }
}