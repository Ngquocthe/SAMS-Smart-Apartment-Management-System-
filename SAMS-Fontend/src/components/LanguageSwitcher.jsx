import React from 'react';
import { useLanguage } from '../hooks/useLanguage';

const LanguageSwitcher = ({ variant = 'default', size = 'medium' }) => {
  const { currentLanguage, changeLanguage } = useLanguage();

  const getButtonClass = (isActive) => {
    let baseClass = 'px-3 py-2 rounded-lg font-medium transition-all duration-200 border flex items-center justify-center gap-2 w-24';
    
    // Size variants
    if (size === 'small') baseClass += ' text-xs px-2 py-1 w-20';
    if (size === 'large') baseClass += ' text-base px-4 py-3 w-28';
    if (size === 'medium') baseClass += ' text-sm px-3 py-2 w-24';
    
    // Style variants - Dark theme for better visibility
    if (variant === 'minimal') {
      baseClass += ' bg-transparent border-transparent hover:bg-gray-100';
    } else if (variant === 'outlined') {
      baseClass += ' bg-slate-700 border-slate-600 hover:bg-slate-600';
    } else if (variant === 'filled') {
      baseClass += ' border-transparent';
    } else {
      baseClass += ' bg-slate-700 border-slate-600 hover:bg-slate-600';
    }
    
    if (isActive) {
      baseClass += ' bg-slate-600 text-white border-slate-500 shadow-lg';
    } else {
      baseClass += ' text-gray-300 hover:text-white';
    }
    
    return baseClass;
  };

  return (
    <div className={`flex gap-2 ${size === 'small' ? 'gap-1' : 'gap-2'}`}>
      <button
        className={getButtonClass(currentLanguage === 'vi')}
        onClick={() => changeLanguage('vi')}
        title="Tiếng Việt"
      >
        <span className="text-lg">🇻🇳</span>
        <span className="text-xs font-medium truncate">VN</span>
      </button>
      <button
        className={getButtonClass(currentLanguage === 'en')}
        onClick={() => changeLanguage('en')}
        title="English"
      >
        <span className="text-lg">🇬🇧</span>
        <span className="text-xs font-medium truncate">EN</span>
      </button>
    </div>
  );
};

    export default LanguageSwitcher;
