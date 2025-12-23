package noah.mapper;

import org.keycloak.models.ClientSessionContext;
import org.keycloak.models.ProtocolMapperModel;
import org.keycloak.models.UserSessionModel;
import org.keycloak.protocol.oidc.OIDCLoginProtocol;
import org.keycloak.protocol.oidc.mappers.*;
import org.keycloak.provider.ProviderConfigProperty;
import org.keycloak.representations.IDToken;

import java.util.ArrayList;
import java.util.List;

public class BuildingIdProtocolMapper
        extends AbstractOIDCProtocolMapper
        implements OIDCAccessTokenMapper, OIDCIDTokenMapper, UserInfoTokenMapper {

    public static final String PROVIDER_ID = "noah-building-id-protocol-mapper";
    public static final String DEFAULT_CLAIM = "building_id";
    public static final String NOTE_BUILDING_ID = "axon.building_id";

    @Override public String getId() { return PROVIDER_ID; }
    @Override public String getProtocol() { return OIDCLoginProtocol.LOGIN_PROTOCOL; }
    @Override public String getDisplayCategory() { return TOKEN_MAPPER_CATEGORY; }
    @Override public String getDisplayType() { return "Building ID (from login)"; }
    @Override public String getHelpText() {
        return "Add building_id claim from session note set by custom authenticator during login.";
    }

    private static final List<ProviderConfigProperty> CONFIG;
    static {
        CONFIG = new ArrayList<>();
        // Cho phép đổi tên claim
        var claim = new ProviderConfigProperty();
        claim.setName(OIDCAttributeMapperHelper.TOKEN_CLAIM_NAME);
        claim.setLabel("Claim name");
        claim.setType(ProviderConfigProperty.STRING_TYPE);
        claim.setDefaultValue(DEFAULT_CLAIM);
        CONFIG.add(claim);

        // Checkbox: add vào Access/ID/UserInfo
        OIDCAttributeMapperHelper.addIncludeInTokensConfig(CONFIG, BuildingIdProtocolMapper.class);
    }
    @Override public List<ProviderConfigProperty> getConfigProperties() { return CONFIG; }

    @Override
    protected void setClaim(IDToken token,
                            ProtocolMapperModel mappingModel,
                            UserSessionModel userSession,
                            org.keycloak.models.KeycloakSession session,
                            ClientSessionContext clientSessionCtx) {

        String val = null;
        if (userSession != null) {
            val = userSession.getNote(NOTE_BUILDING_ID);
        }
        if (val == null && clientSessionCtx != null && clientSessionCtx.getClientSession() != null) {
            val = clientSessionCtx.getClientSession().getNote(NOTE_BUILDING_ID);
        }
        if (val != null && !val.isBlank()) {
            OIDCAttributeMapperHelper.mapClaim(token, mappingModel, val);
        }
    }
}
