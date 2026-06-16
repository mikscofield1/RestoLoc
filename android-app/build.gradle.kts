plugins {
    kotlin("android") version "1.8.20" apply false
    id("com.android.application") version "8.1.0" apply false
}

// Use repositories
allprojects {
    repositories {
        google()
        mavenCentral()
    }
}
