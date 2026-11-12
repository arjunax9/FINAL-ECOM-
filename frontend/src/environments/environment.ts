export const environment = {
  production: false,
  apiUrl: typeof window !== 'undefined' ? `http://${window.location.hostname}:5000/api` : 'http://localhost:5000/api'
};
