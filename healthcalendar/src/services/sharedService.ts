

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api';

// Helper to get auth token from localStorage
export function getAuthToken(): string | null {
  return localStorage.getItem('hc_token');
}

// Helper to create headers with auth token
// Adds Authorization header with Bearer token for authenticated requests
export function getHeaders(): HeadersInit {
  const token = getAuthToken();
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

// Helper to handle API responses
// Throws descriptive errors for HTTP failures, parses JSON responses
export async function handleResponse<T>(response: Response): Promise<T> {
	if (!response.ok) {
		const errorText = await response.text();
		if (response.status === 406) {
			throw new Error('Request not acceptable: ' + errorText);
		} else if (response.status === 500) {
			throw new Error('Server error: ' + errorText);
		} else if (response.status === 401) {
			throw new Error('Unauthorized. Please log in again.');
		} else {
			throw new Error(`HTTP ${response.status}: ${errorText}`);
		}
	}
	const contentType = response.headers.get('content-type');
	if (contentType && contentType.includes('application/json')) {
		return await response.json();
	}
	return {} as T;
}