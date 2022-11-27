const fs = require('fs');

// \n at the end cause we don't put one in the template
const prefix = '<?xml version="1.0" encoding="utf-8" ?>\n<LanguageData>\n';

// No need for \n cause it's in the template
const suffix = '</LanguageData>';

// Two spaces for indentation wow
const template = '  <Skill{{num}}>{{desc}}</Skill{{num}}>\n';

let dataToWrite = '';

// I made this in 5 minutes, please someone PR this shit into a better solution cause it's awful
const desc = [
  'Transcended',
  'Transcended Expert',
  'Transcended Master',
  'Transcended Legend',
  'Demigod',
  'Rookie God',
  'God',
  'Expert God',
  'Master God',
  'God of Gods',
  'Transcended God',
  'God of Transcended Gods',
  'Transcended God of Doom',
  'God of Transcended Gods of Doom',
  'Transcended God of Transcended Doom',
  'God of Transcended Gods of Transcended Doom',
  'Transcended God of Transcended Gods of Transcended Doom',
]

// Prefix stuff
dataToWrite += prefix;

// A counter for the string to choose
let d = 0;
// We stop at 99 cause we want the last one to be different
for (let i = 21; i < 100; i++) {
  // Make it so that it chooses a string every 5 entries starting at 0 for the first 5
  if (i - 25 > d * 5) {
    d++;
    // Let's slap a space to better see the transition points
    dataToWrite += '\n';
  }
  dataToWrite += template.replaceAll('{{num}}', i).replaceAll('{{desc}}', desc[d]);
}

// 100 is better cause why not
dataToWrite += '\n';
dataToWrite += template.replaceAll('{{num}}', 100).replaceAll('{{desc}}', desc[desc.length - 1]);

// Suffix stuff
dataToWrite += suffix;

// We write it in the same folder cause I'm not trusting myself enough
// Manual verification and then copy paste is better :)
fs.writeFileSync('Skills.xml', dataToWrite);