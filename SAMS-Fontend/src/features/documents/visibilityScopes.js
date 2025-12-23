// VisibilityScope enum mapping từ backend
export const VisibilityScope = {
    Public: 'Public',
    Accounting: 'Accounting',
    Receptionist: 'Receptionist',
    Resident: 'Resident',

};

// Nhãn tiếng Việt cho UI
export const VisibilityScopeLabels = {
    [VisibilityScope.Public]: 'Công khai (Tất cả)',
    [VisibilityScope.Accounting]: 'Kế toán',
    [VisibilityScope.Receptionist]: 'Lễ tân',
    [VisibilityScope.Resident]: 'Cư dân',
};

const PRIMARY_SCOPES = [
    VisibilityScope.Public,
    VisibilityScope.Accounting,
    VisibilityScope.Receptionist,
    VisibilityScope.Resident,
];

// Helper functions
export const getScopeName = (scopeValue) => {
    return scopeValue || 'Không xác định';
};

export const getScopeLabel = (scopeValue) => {
    if (!scopeValue) return VisibilityScopeLabels[VisibilityScope.Public];
    return VisibilityScopeLabels[scopeValue] || scopeValue;
};

export const getScopeOptions = () => {
    return PRIMARY_SCOPES.map((value) => ({
        value,
        label: VisibilityScopeLabels[value]
    }));
};

// Convert string scope to enum value
export const stringToScopeEnum = (scopeString) => {
    return scopeString || VisibilityScope.Public;
};

// Convert enum value to string
export const scopeEnumToString = (scopeValue) => {
    return scopeValue || VisibilityScope.Public;
};
