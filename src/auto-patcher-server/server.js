const express = require('express');
const multer = require('multer');
const fs = require('fs');
const path = require('path');
const bodyParser = require('body-parser');
const app = express();
const port = 1338;

// Use EJS for templating
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

// Middleware to parse form data
app.use(bodyParser.urlencoded({ extended: true }));

const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    cb(null, 'patches/')
  },
  filename: (req, file, cb) => {
    fs.readdir('patches/', (err, files) => {
      if (err) {
        console.error('Could not list the directory.', err);
        process.exit(1);
      }

      // Extract the version numbers from filenames, assuming the format is "10001.zip"
      let versions = files.map(file => parseInt(file.split('.')[0])).filter(num => !isNaN(num));

      // Find the maximum version number
      let maxVersion = versions.length > 0 ? Math.max(...versions) : 10000;
      cb(null, `${maxVersion + 1}.zip`);
    });
  }
});

const upload = multer({ storage: storage });
const HARDCODED_USERNAME = '132hb123h12u';
const HARDCODED_PASSWORD = 'h2i12huifhh2';
let authenticated = false;

app.get('/', (req, res) => {
  if (authenticated) {
    fs.readdir('patches/', (err, files) => {
      if (err) {
        console.error('Could not list the directory.', err);
        res.sendStatus(500);
      } else {
        res.render('dashboard', { files });
      }
    });
  } else {
    res.render('login', { error: null });
  }
});

app.get('/download/:version', (req, res) => {
  const version = req.params.version;
  const filePath = path.join(__dirname, 'patches', `${version}.zip`);

  fs.access(filePath, fs.constants.F_OK, (err) => {
    if (err) {
      console.error('File does not exist:', filePath);
      return res.sendStatus(404); // Not Found
    }
    console.log('Sending file:', filePath);
    // File exists, send it as a response
    res.download(filePath, (err) => {
      if (err) {
        // Handle errors, but don't leak the filesystem path
        console.error('Error sending file:', err);
        if (!res.headersSent) {
          res.sendStatus(500); // Internal Server Error
        }
      }
    });
  });
});

app.get('/version', (req, res) => {
  fs.readdir('patches/', (err, files) => {
    if (err) {
      console.error('Could not list the directory.', err);
      return res.sendStatus(500); // Internal Server Error
    }

    let versions = files
      .map(file => parseInt(file.split('.')[0], 10))
      .filter(num => !isNaN(num));

    let currentVersion = versions.length > 0 ? Math.max(...versions) : 10000;

    res.json({ version: currentVersion });
  });
});


app.post('/login', (req, res) => {
  const { username, password } = req.body;
  if (username === HARDCODED_USERNAME && password === HARDCODED_PASSWORD) {
    authenticated = true;
    res.redirect('/');
  } else {
    res.render('login', { error: 'Invalid Credentials' });
  }
});

app.post('/upload', upload.single('patch'), (req, res) => {
  if (authenticated) {
    res.redirect('/');
  } else {
    res.sendStatus(403);
  }
});

app.post('/delete', (req, res) => {
  if (authenticated) {
    const { filename } = req.body;
    const filePath = path.join('patches', filename);
    fs.unlink(filePath, (err) => {
      if (err) {
        console.error('Could not delete file', err);
        res.sendStatus(500);
      } else {
        res.redirect('/');
      }
    });
  } else {
    res.sendStatus(403);
  }
});

// ...rest of the routes

app.listen(port, () => {
  console.log(`Server running at http://localhost:${port}`);
});
