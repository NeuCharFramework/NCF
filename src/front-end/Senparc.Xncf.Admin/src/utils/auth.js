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

// isHaveToken();
export function isHaveToken() {
  const hasToken = getToken();
  // console.log('判断token', hasToken);
  if (hasToken == null || hasToken == undefined || !hasToken) {
    // console.log('已过期，退出登录');
    Message({
      message: "登录超时请重新登录",
      type: "error",
      duration: 5 * 1000,
    });
    window.location.replace = `/login?redirect=${router.history.current.fullPath}`;
    removeToken();
    removeRole();
    return;
  } 
  // else {
  //   console.log('未过期，正常状态');
  // }
}
