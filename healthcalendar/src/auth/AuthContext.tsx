import React, { createContext, useState, useContext, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { JwtUser } from '../types/user';
import type { LoginDto } from '../types/auth';
import * as authService from './AuthService';

interface AuthContextType {
    user: JwtUser | null;
    token: string | null;
    loginPatient: (credentials: LoginDto) => Promise<JwtUser>;
    loginWorker: (credentials: LoginDto) => Promise<JwtUser>;
    logout: () => void;
    isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<JwtUser | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem('hc_token'));
    const [isLoading, setIsLoading] = useState<boolean>(true);

    useEffect(() => {
        if (token) {
            try {
                const decodedUser: JwtUser = authService.decodeUser(token);
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

    // Internal: perform login call and decode without committing to state/storage
    const loginRaw = async (credentials: LoginDto): Promise<{ token: string; decodedUser: JwtUser }> => {
        const { token } = await authService.login(credentials);
        const decodedUser: JwtUser = authService.decodeUser(token);
        return { token, decodedUser };
    };

    // Patient-only login from patient form
    const loginPatient = async (credentials: LoginDto): Promise<JwtUser> => {
        const { token, decodedUser } = await loginRaw(credentials);
        if (decodedUser.role !== 'Patient') {
            // Do not commit token for wrong role
            throw new Error('Please use the personnel login for this account.');
        }
        localStorage.setItem('hc_token', token);
        setUser(decodedUser);
        setToken(token);
        return decodedUser;
    };

    // Worker/Usermanager-only login from worker form
    const loginWorker = async (credentials: LoginDto): Promise<JwtUser> => {
        const { token, decodedUser } = await loginRaw(credentials);
        if (decodedUser.role !== 'Worker' && decodedUser.role !== 'Usermanager') {
            // Do not commit token for wrong role
            throw new Error('Please use the patient login for this account.');
        }
        localStorage.setItem('hc_token', token);
        setUser(decodedUser);
        setToken(token);
        return decodedUser;
    };

    const logout = () => {
        // Option A: keep mock events across logouts
        authService.logout()
        localStorage.removeItem('hc_token');
        setUser(null);
        setToken(null);
    };

    return ( // Provide context to children
        <AuthContext.Provider value={{ user, token, loginPatient, loginWorker, logout, isLoading }}>
            {!isLoading && children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => { // Custom hook to use auth context
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};