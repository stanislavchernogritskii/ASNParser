# Readme.md

## How to Deploy the Application with Docker

1. **Prerequisites**: Ensure you have Docker and Docker Compose installed on your machine. 

2. **Clone the repository**: Clone the repository to your local machine using the command: `git clone <repository-url>`.

3. **Build the Docker image**: Navigate to the project directory and run the command `docker-compose build`.

4. **Run the Docker container**: Run the Docker container in the detached mode using the command `docker-compose up -d`.

## How to Use the Application

The application is composed of two services.

1. **asnparser**: This service is the main application that processes files. It depends on the `db` service to be operational. It has a volume mount that maps the local `ASNParser` directory to the `/app/ASNParser` directory inside the Docker container.

2. **db**: This service is a PostgreSQL database. It uses the `postgres:13` image from Docker Hub. It has a volume mount that maps the local `util` directory to the `/docker-entrypoint-initdb.d` directory inside the Docker container, and another volume `postgres-data` for persisting the database data. In the `util` directory, there is a `db_setup.sql` file that contains the SQL commands to create tables to store the parsed data.

The services communicate with each other over a Docker network named `app-network`.

To test the service you can copya and paste any `.txt` file with the structure like
<pre>
HDR  TRSP117                                           6874454I                           
LINE P000001661         9781465121550         12     
LINE P000001661         9925151267712         2      
LINE P000001661         9651216865465         1      
</pre>

Content of the file should be parsed and stored in the database. You can check the database using the command `docker-compose exec db psql -U myuser -d mydatabase -c "SELECT * FROM box_content;"`.

