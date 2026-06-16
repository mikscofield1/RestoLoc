RestoLoc Android (Jetpack Compose) - Minimal scaffold

Steps to run

1. Open the folder `android-app` in Android Studio (recommended).
2. Let Gradle sync.
3. Build and run on an emulator or physical device.

Notes
- The app uses the Supabase REST API. `SUPABASE_URL` and `SUPABASE_KEY` are injected into `BuildConfig` via `app/build.gradle.kts`.
- For production, do NOT include a `service_role` key in the client. Use RLS and server-side endpoints when necessary.
- If Retrofit DELETE with query params fails, adjust the API to use `@HTTP` or custom OkHttp calls.

If you want, I can:
- Add Add/Edit screens
- Create a proper Retrofit DELETE implementation
- Prepare an APK for installation
