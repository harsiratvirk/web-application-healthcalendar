// Minimal User type aligned with example; use decoded token directly where needed
export interface User {
    sub: string
    email: string
    nameid: string
    jti: string
    iat: number
    exp: number
    iss: string
    aud: string
    // optional role & workerId still possible in token but not required in this minimal type
    [k: string]: any
}