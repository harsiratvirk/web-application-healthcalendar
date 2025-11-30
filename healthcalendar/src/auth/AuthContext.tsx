import React, { createContext, useState, useContext, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { JwtUser } from '../types/user';
import type { LoginDto } from '../types/auth';
import * as authService from './AuthService';

// Authentication context provider for managing user authentication state

// Define the shape of the authentication context
interface AuthContextType {
    user: JwtUser | null;
    token: string | null;
    loginPatient: (credentials: LoginDto) => Promise<JwtUser>;
    loginWorker: (credentials: LoginDto) => Promise<JwtUser>;
    logout: () => void;
    isLoading: boolean;
}

// Create the context with undefined as default
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// AuthProvider component wraps the app and provides authentication state
export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<JwtUser | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem('hc_token'));
    const [isLoading, setIsLoading] = useState<boolean>(true);

    // On mount or token change, validate and restore authentication state
    useEffect(() => {
        if (token) {
            try {
                // Decode JWT token to extract user information
                const decodedUser: JwtUser = authService.decodeUser(token);
                // Check if token is still valid (not expired)
                if (!decodedUser.exp || decodedUser.exp * 1000 > Date.now()) {
                    setUser(decodedUser);
                } else {
                    // Token is expired, clear it
                    localStorage.removeItem('hc_token');
                    localStorage.removeItem('hc_events'); // clear stale patient event cache on token expiry
                    setUser(null);
                    setToken(null);
                }
            } catch (error) {
                // Any decoding error -> clear auth state
                localStorage.removeItem('hc_token');
                localStorage.removeItem('hc_events'); // decoding error -> purge events
                setUser(null);
                setToken(null);
            }
        } else {
            // No token at startup: preserve local mock events (Option A)
            setUser(null);
            setToken(null);
        }
        setIsLoading(false);
    }, [token]);

    // Internal helper: perform login call and decode JWT without committing to state/storage
    const loginRaw = async (credentials: LoginDto): Promise<{ token: string; decodedUser: JwtUser }> => {
        const { token } = await authService.login(credentials);
        const decodedUser: JwtUser = authService.decodeUser(token);
        return { token, decodedUser };
    };

    // Patient-specific login handler - validates role before committing authentication
    const loginPatient = async (credentials: LoginDto): Promise<JwtUser> => {
        const { token, decodedUser } = await loginRaw(credentials);
        // Enforce role-based access: only patients can use patient login
        if (decodedUser.role !== 'Patient') {
            throw new Error('Please use the personnel login for this account.');
        }
        // Store token and update state on successful patient login
        localStorage.setItem('hc_token', token);
        setUser(decodedUser);
        setToken(token);
        return decodedUser;
    };

    // Worker/Admin-only login from worker form
    const loginWorker = async (credentials: LoginDto): Promise<JwtUser> => {
        const { token, decodedUser } = await loginRaw(credentials);
        if (decodedUser.role !== 'Worker' && decodedUser.role !== 'Admin') {
            // Do not commit token for wrong role
            throw new Error('Please use the patient login for this account.');
        }
        // Store token and update state on successful worker/Admin login
        localStorage.setItem('hc_token', token);
        setUser(decodedUser);
        setToken(token);
        return decodedUser;
    };

    // Clear authentication state and remove token from storage
    const logout = () => {
        // Option A: keep mock events across logouts
        authService.logout()
        localStorage.removeItem('hc_token');
        setUser(null);
        setToken(null);
    };

    // Provide authentication context to all child components
    return (
        <AuthContext.Provider value={{ user, token, loginPatient, loginWorker, logout, isLoading }}>
            {/* Only render children after initial auth check is complete */}
            {!isLoading && children}
        </AuthContext.Provider>
    );
};

// Custom hook to access authentication context from any component
export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    // Ensure hook is used within AuthProvider
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};