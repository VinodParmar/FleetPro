const sharp=require('sharp');const fs=require('fs');
(async()=>{const files=fs.readdirSync('render').filter(f=>/^s\d+\.png$/.test(f)).sort();
for(let g=0;g*6<files.length;g++){const part=files.slice(g*6,g*6+6);
 const comps=await Promise.all(part.map(async(f,i)=>({input:await sharp('render/'+f).resize(800,450).toBuffer(),left:(i%2)*810,top:Math.floor(i/2)*460})));
 await sharp({create:{width:1620,height:1380,channels:3,background:'#888'}}).composite(comps).png().toFile(`render/grid${g+1}.png`);}
console.log('ok')})();
