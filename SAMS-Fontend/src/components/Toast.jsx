import React, { useEffect } from 'react';
import { Alert } from 'react-bootstrap';

export default function Toast({ message, show, onClose, type = 'success', duration = 3000 }) {
  useEffect(() => {
    if (show) {
      const closeTimer = setTimeout(() => {
        onClose();
      }, duration);

      return () => {
        clearTimeout(closeTimer);
      };
    }
  }, [show, onClose, duration]);

  if (!show) return null;

  const getVariant = () => {
    switch (type) {
      case 'success':
        return 'success';
      case 'error':
        return 'danger';
      case 'warning':
        return 'warning';
      case 'info':
        return 'info';
      default:
        return 'success';
    }
  };

  return (
    <div 
      className="position-fixed top-0 start-0 end-0 d-flex justify-content-center mt-4"
      style={{ zIndex: 9999 }}
    >
      <Alert 
        variant={getVariant()} 
        dismissible 
        onClose={onClose}
        style={{ minWidth: '350px', maxWidth: '500px' }}
      >
        {message}
      </Alert>
    </div>
  );
}
