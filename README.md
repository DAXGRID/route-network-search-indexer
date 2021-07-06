# Route network search indexer

Route node search indexer consumes messages from Kafka in the OPEN-FTTH solution on topic `domain.route-network` and inserts the ones with `Naming.Name` set into Typesense. If there is any updates to an already inserted feature it will be either, updated or deleted.
