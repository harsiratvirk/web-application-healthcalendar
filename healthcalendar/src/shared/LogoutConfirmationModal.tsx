// Logout confirmation modal component
import React from 'react';
import { useAuth } from '../auth/AuthContext';

interface LogoutConfirmationModalProps {
    isOpen: boolean;
    onClose: () => void;
}

const LogoutConfirmationModal: React.FC<LogoutConfirmationModalProps> = ({ isOpen, onClose }) => {
    const { logout } = useAuth();

    if (!isOpen) return null;

    const handleConfirm = () => {
        logout();
        window.location.href = '/';
    };

    return (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="logout-confirm-title" aria-describedby="logout-confirm-desc">
            <div className="modal confirm-modal">
                <header className="modal__header">
                    <h2 id="logout-confirm-title">Confirm Logout</h2>
                    <button className="icon-btn" onClick={onClose} aria-label="Close confirmation">
                        <img src="/images/exit.png" alt="Close" />
                    </button>
                </header>
                <div id="logout-confirm-desc" className="confirm-body">
                    Are you sure you want to log out?
                </div>
                <div className="confirm-actions">
                    <button type="button" className="btn" onClick={onClose}>Cancel</button>
                    <button
                        type="button"
                        className="btn btn--primary"
                        onClick={handleConfirm}
                    >
                        Confirm
                    </button>
                </div>
            </div>
        </div>
    );
};

export default LogoutConfirmationModal;