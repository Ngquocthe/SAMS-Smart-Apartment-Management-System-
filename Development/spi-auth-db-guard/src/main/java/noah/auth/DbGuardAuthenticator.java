package noah.auth;

import jakarta.ws.rs.core.Response;
import org.keycloak.authentication.*;
import org.keycloak.models.*;
import org.keycloak.models.utils.FormMessage;

import java.sql.*;
import java.util.List;

import org.jboss.logging.Logger;
import org.keycloak.models.utils.KeycloakModelUtils;

public class DbGuardAuthenticator implements Authenticator {

    public static final String NOTE_BUILDING_ID = "axon.building_id";

    private static final Logger LOG = Logger.getLogger(DbGuardAuthenticator.class);

    private String jdbcUrl() {
        return System.getenv().getOrDefault(
                "AXON_JDBC_URL",
                "jdbc:sqlserver://mssql:1433;databaseName=BuildingManagement;encrypt=false;trustServerCertificate=true"
        );
    }

    private String jdbcUser() {
        return System.getenv().getOrDefault("AXON_JDBC_USER", "SA");
    }

    private String jdbcPass() {
        return System.getenv().getOrDefault("AXON_JDBC_PASSWORD", "@Anhngoc2003");
    }

    private String adminClientId() {
        return System.getenv().getOrDefault("AXON_ADMIN_CLIENT_ID", "backend");
    }

    private String adminRoleName() {
        return System.getenv().getOrDefault("AXON_ADMIN_ROLE", "global_admin");
    }

    @Override
    public void authenticate(AuthenticationFlowContext ctx) {
        var params = ctx.getHttpRequest().getDecodedFormParameters();
        if (params == null) {
            ctx.attempted();
            return;
        }

        String usernameOrEmail = trimOrNull(params.getFirst("username"));
        String buildingSchema = trimOrNull(params.getFirst("buildingSchema"));

        if (usernameOrEmail == null || buildingSchema == null) {
            var challenge = ctx.form()
                    .setErrors(List.of(new FormMessage(null, "missingUsernameOrBuilding")))
                    .createLoginUsernamePassword();
            ctx.failureChallenge(AuthenticationFlowError.INVALID_USER, challenge);
            return;
        }

        if (hasAdminClientRole(ctx.getSession(), ctx.getRealm(), usernameOrEmail)) {
            ctx.getAuthenticationSession().setUserSessionNote(NOTE_BUILDING_ID, "GLOBAL");
            LOG.infof("Bypass building check for admin client role: usernameOrEmail=%s", usernameOrEmail);
            ctx.success();
            return;
        }

        LOG.infof("Your user name or email" + usernameOrEmail);
        LOG.infof("DbGuardAuthenticator check: usernameOrEmail=%s, buildingSchema=%s", usernameOrEmail, buildingSchema);

        boolean ok = false;
        try (Connection con = DriverManager.getConnection(jdbcUrl(), jdbcUser(), jdbcPass());
             PreparedStatement ps = con.prepareStatement(
                     "SELECT TOP 1 1 " +
                             "FROM dbo.user_registry ur WITH (NOLOCK) " +
                             "JOIN dbo.user_building ub WITH (NOLOCK) ON ub.keycloak_user_id = ur.keycloak_user_id " +
                             "JOIN dbo.building bd WITH (NOLOCK) ON bd.id = ub.building_id " +
                             "WHERE bd.schema_name = ? " +
                             "  AND (ur.username = ? OR ur.email = ?) " +
                             "  AND ur.status = 1 AND ub.status = 1 AND bd.status = 1"
             )) {

            ps.setQueryTimeout(5);
            ps.setString(1, buildingSchema);
            ps.setString(2, usernameOrEmail);
            ps.setString(3, usernameOrEmail);

            try (ResultSet rs = ps.executeQuery()) {
                ok = rs.next();
            }
        } catch (SQLException e) {
            LOG.error("DbGuard SQL error (building check)", e);
            var challenge = ctx.form()
                    .setErrors(List.of(new FormMessage(null, "internalError")))
                    .createErrorPage(Response.Status.INTERNAL_SERVER_ERROR);
            ctx.failureChallenge(AuthenticationFlowError.INTERNAL_ERROR, challenge);
            return;
        }

        if (!ok) {
            LOG.warnf("User not in building or inactive: usernameOrEmail=%s, buildingSchema=%s", usernameOrEmail, buildingSchema);
            var challenge = ctx.form()
                    .setErrors(List.of(new FormMessage("username", "Invalid selected building")))
                    .createLoginUsernamePassword();
            ctx.failureChallenge(AuthenticationFlowError.INVALID_USER, challenge);
            return;
        }

        ctx.getAuthenticationSession().setUserSessionNote("axon.building_id", buildingSchema);

        ctx.success();
    }

    private boolean hasAdminClientRole(KeycloakSession session, RealmModel realm, String usernameOrEmail) {
        UserModel user = KeycloakModelUtils.findUserByNameOrEmail(session, realm, usernameOrEmail);
        if (user == null) {
            LOG.debugf("User not found when checking admin client role: %s", usernameOrEmail);
            return false;
        }

        String clientId = adminClientId();
        String roleName = adminRoleName();

        ClientModel client = realm.getClientByClientId(clientId);
        if (client == null) {
            LOG.warnf("Admin client not found (clientId=%s).", clientId);
            return false;
        }

        RoleModel role = client.getRole(roleName);
        if (role == null) {
            LOG.warnf("Admin role not found on client (clientId=%s, roleName=%s).", clientId, roleName);
            return false;
        }

        boolean has = user.hasRole(role);
        LOG.debugf("hasAdminClientRole? user=%s clientId=%s role=%s => %s", usernameOrEmail, clientId, roleName, has);
        return has;
    }

    private static String trimOrNull(String s) {
        if (s == null) return null;
        s = s.trim();
        return s.isEmpty() ? null : s;
    }

    @Override
    public void action(AuthenticationFlowContext ctx) {
        authenticate(ctx);
    }

    @Override
    public boolean requiresUser() {
        return false;
    }

    @Override
    public boolean configuredFor(KeycloakSession s, RealmModel r, UserModel u) {
        return true;
    }

    @Override
    public void setRequiredActions(KeycloakSession s, RealmModel r, UserModel u) {
    }

    @Override
    public void close() {
    }
}