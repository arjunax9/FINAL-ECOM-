export const environment = {
  production: true,
  apiUrl: typeof window !== 'undefined' ? `http://${window.location.hostname}:5000/api` : 'http://localhost:5000/api'
};
