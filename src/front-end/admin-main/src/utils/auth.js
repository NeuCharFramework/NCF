import Cookies from 'js-cookie'

const TokenKey = 'Admin-Token'
const RoleKey = 'Admin-Role'
export function getToken() {
  return Cookies.get(TokenKey)
}

export function setToken(token) {
  return Cookies.set(TokenKey, token)
}

export function removeToken() {
  return Cookies.remove(TokenKey)
}
export function getRole() {
  const r = Cookies.get(RoleKey) || '[]'
  return JSON.parse(r)
}

export function setRole(role) {
  return Cookies.set(RoleKey, JSON.stringify(role))
}

export function removeRole() {
  return Cookies.remove(RoleKey)
}
