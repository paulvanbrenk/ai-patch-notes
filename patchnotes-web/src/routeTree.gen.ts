import { rootRoute } from './routes/__root';
import { indexRoute } from './routes/index';
import { adminRoute } from './routes/admin';
import { packageDetailRoute } from './routes/packages.$packageId';
import { releaseDetailRoute } from './routes/releases.$releaseId';

export const routeTree = rootRoute.addChildren([
  indexRoute,
  adminRoute,
  packageDetailRoute,
  releaseDetailRoute,
]);
