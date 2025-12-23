// DocumentCategory enum mapping từ backend (sử dụng string values)
export const DocumentCategory = {
    Administrative: 'Administrative',
    Financial: 'Financial',
    Technical: 'Technical',
    Legal: 'Legal',
    Resident: 'Resident',
};

// Nhãn tiếng Việt cho UI
export const DocumentCategoryLabels = {
    [DocumentCategory.Administrative]: 'Hành chính',
    [DocumentCategory.Financial]: 'Tài chính',
    [DocumentCategory.Technical]: 'Kỹ thuật',
    [DocumentCategory.Legal]: 'Pháp lý',
    [DocumentCategory.Resident]: 'Tài liệu cư dân',
};

// Helper functions
export const getCategoryLabel = (categoryValue) => {
    return DocumentCategoryLabels[categoryValue] || 'Không xác định';
};

export const getCategoryOptions = () => {
    return Object.entries(DocumentCategoryLabels).map(([value, label]) => ({
        value: value,
        label: label
    }));
};

// Convert string category to enum value
export const stringToCategoryEnum = (categoryString) => {
    return categoryString || DocumentCategory.Administrative;
};
