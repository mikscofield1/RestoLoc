package com.restoloc.app.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.restoloc.app.data.Restaurant
import com.restoloc.app.data.RestaurantRepository
import kotlinx.coroutines.launch

@Composable
fun MainScreen() {
    val repo = remember { RestaurantRepository() }
    val scope = rememberCoroutineScope()
    var items by remember { mutableStateOf(listOf<Restaurant>()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        try {
            loading = true
            items = repo.fetchAll()
        } catch (e: Exception) {
            error = e.message
        } finally {
            loading = false
        }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text("RestoLoc") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { /* TODO: add */ }) {
                Text("+")
            }
        }
    ) { padding ->
        Box(modifier = Modifier.padding(padding)) {
            if (loading) {
                CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
            } else if (error != null) {
                Text("Erreur: $error", color = MaterialTheme.colors.error, modifier = Modifier.padding(16.dp))
            } else {
                LazyColumn(modifier = Modifier.fillMaxSize().padding(8.dp)) {
                    items(items) { resto ->
                        Card(modifier = Modifier.fillMaxWidth().padding(4.dp)) {
                            Column(modifier = Modifier.padding(8.dp)) {
                                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                                    Text(resto.nom ?: "")
                                    Text(if (resto.estFiable == true) "Fiable" else "Non fiable")
                                }
                                Spacer(Modifier.height(4.dp))
                                Text("🍗 Poulet : ${resto.originePoulet} | 🥩 Viande : ${resto.origineViande}")
                                Spacer(Modifier.height(8.dp))
                                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) {
                                    TextButton(onClick = {
                                        scope.launch {
                                            if (resto.id != null) {
                                                val ok = repo.delete(resto.id)
                                                if (ok) items = repo.fetchAll()
                                            }
                                        }
                                    }) { Text("Supprimer") }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
