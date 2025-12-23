import { useState, useEffect } from 'react';
import strings from './LanguageStrings';

export const useLanguage = () => {
  const [currentLanguage, setCurrentLanguage] = useState(strings.getLanguage());

  useEffect(() => {
    // Set initial language from localStorage
    const savedLanguage = localStorage.getItem('language') || 'vi';
    strings.setLanguage(savedLanguage);
    setCurrentLanguage(savedLanguage);

    // Listen for language changes
    const handleLanguageChange = () => {
      setCurrentLanguage(strings.getLanguage());
    };

    // Create a custom event listener for language changes
    window.addEventListener('languageChanged', handleLanguageChange);

    return () => {
      window.removeEventListener('languageChanged', handleLanguageChange);
    };
  }, []);

  const changeLanguage = (lng) => {
    strings.setLanguage(lng);
    setCurrentLanguage(lng);
    localStorage.setItem('language', lng);
    // Dispatch custom event to notify other components
    window.dispatchEvent(new CustomEvent('languageChanged', { detail: lng }));
  };

  return {
    currentLanguage,
    changeLanguage,
    strings
  };
};
