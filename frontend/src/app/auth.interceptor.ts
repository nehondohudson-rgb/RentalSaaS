import { API_BASE_URL } from './api.config';
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = sessionStorage.getItem('rentalsaas_access_token');

  if (!token || !req.url.startsWith(`${API_BASE_URL}/`)) {
    return next(req);
  }

  return next(req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  }));
};
