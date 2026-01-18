import { rootRoute } from './routes/__root';
import { indexRoute } from './routes/index';
import { adminRoute } from './routes/admin';

export const routeTree = rootRoute.addChildren([indexRoute, adminRoute]);
