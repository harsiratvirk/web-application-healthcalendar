import type { LoginDto, RegisterPatientDto } from '../types/auth';
import type { JwtUser } from '../types/user';
import { jwtDecode } from 'jwt-decode';

// Authentication service for handling login, registration, and JWT token operations

// API base URL from environment variable, fallback to localhost
const API_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5080';

// Authenticate user and retrieve JWT token
export const login = async (credentials: LoginDto): Promise<{ token: string }> => {
    const response = await fetch(`${API_URL}/api/Auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        body: JSON.stringify(credentials),
    });
    if (!response.ok) {
        throw new Error('Login failed');
    }
    const data: any = await response.json();
    // Support both "Token" (PascalCase from backend) and "token" (camelCase)
    const token: string | undefined = data?.token ?? data?.Token;
    if (!token) {
        throw new Error('Login failed');
    }
    return { token };
};

// Patient registration endpoint (role enforcement handled by AuthContext)

// Register a new patient account
export const registerPatient = async (userData: RegisterPatientDto): Promise<any> => {
    const response = await fetch(`${API_URL}/api/Auth/registerPatient`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        body: JSON.stringify(userData),
    });
    if (!response.ok) {
        try {
            // Parse backend validation errors if available
            const errorData = await response.json();
            if (Array.isArray(errorData)) {
                const messages = errorData.map((e: any) => e?.description || e?.Description || String(e)).join(', ');
                throw new Error(messages || 'Registration failed');
            }
        } catch {}
        throw new Error('Registration failed');
    }
    return response.json();
};

// Logout function to notify backend and clear session
// Note: Primary logout handling (clearing localStorage) is done in AuthContext
export const logout = async () => {
    // Retrieve token to include in request
    const token = localStorage.getItem('hc_token')
    const headers: HeadersInit = {
        'Content-Type': 'application/json', 
        'Accept': 'application/json'
    }
    // Include authorization header if token exists
    if (token) headers['Authorization'] = `Bearer ${token}`

    const response = await fetch(`${API_URL}/api/Auth/logout`, {
        method: 'POST',
        headers: headers
    });
    if (!response.ok) {
        // Parse and throw backend error messages
        const errorData = await response.json();
            if (Array.isArray(errorData)) {
                const messages = errorData.map((e: any) => e?.description || e?.Description || String(e)).join(', ');
                throw new Error(messages || 'Registration failed');
            }
    }
}

// Decode and normalize JWT token claims to frontend user object
// Separated as a helper so AuthContext can reuse without re-implementing logic
export function decodeUser(token: string): JwtUser {
    try {
        const raw: any = jwtDecode(token);
        
        // Normalize ASP.NET Core claim types to simplified frontend format
        const role = raw.role ?? raw["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        const nameid = raw.nameid ?? raw["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
        const name = raw.name; // JwtRegisteredClaimNames.Name -> "name"
        const jti = raw.jti;
        const iat = typeof raw.iat === 'string' ? parseInt(raw.iat, 10) : raw.iat;
        
        // Build normalized user object with property names
        const normalized: any = {
            sub: raw.sub,
            name,
            nameid,
            role,
            jti,
            iat,
            exp: raw.exp,
        };
        
        // Include WorkerId if present (only on Patient tokens for assigned worker reference)
        if (typeof raw.WorkerId !== 'undefined') normalized.WorkerId = String(raw.WorkerId);
        
        return normalized as JwtUser;
    } catch (e) {
        throw new Error('Failed to decode token');
    }
}